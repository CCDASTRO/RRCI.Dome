// ======================================================
// ASCOM Roll-Off Roof Controller Firmware
// Single Relay Toggle Version
// ACTIVE HIGH RELAY VERSION
//
// Chuck Faranda - https://ccdastro.net
//
// Features:
// - Optional scope safety input
// - Optional Hall-effect motion sensor
// - Runtime enable/disable of both features
// - OPEN/CLOSED limit switch support
// - Pulse counting for roof percentage tracking
// - Pulse polling support for ASCOM driver
// - Stall timeout protection
// - ASCOM-compatible serial protocol
// - Robust serial command parser with reconnect recovery
// ======================================================

#include <string.h>

// ======================================================
// PINS
// ======================================================

#define PIN_OPENED         11
#define PIN_CLOSED         12
#define PIN_SAFE           13
#define PIN_MOTION          2

#define RELAY_TRIGGER       7
#define RELAY_UNUSED        6
#define RELAY_SENSOR        5
#define RELAY_SPARE         4

#define LED_PIN            10

// ======================================================
// SENSOR POLARITY
// ======================================================

#define OPEN_ACTIVE       LOW
#define CLOSE_ACTIVE      LOW
#define SAFE_ACTIVE       LOW
#define MOTION_ACTIVE     LOW

// ======================================================
// TIMING
// ======================================================

const unsigned long MOVE_TIMEOUT            = 60000UL;
const unsigned long RELAY_PULSE_TIME        = 500UL;
const unsigned long MOTION_CHECK_INTERVAL   = 3000UL;
const unsigned long SERIAL_COMMAND_TIMEOUT  = 1000UL;

// ======================================================
// STATES
// ======================================================

enum RoofState
{
  IDLE,
  OPENING,
  CLOSING,
  OPEN,
  CLOSED,
  ERROR
};

RoofState state = IDLE;

// ======================================================
// GLOBALS
// ======================================================

unsigned long moveStart = 0;
unsigned long lastMotionTime = 0;
unsigned long lastSerialByteTime = 0;

volatile unsigned long motionPulseCount = 0;

bool safeModeEnabled = false;
bool motionSensorEnabled = false;

bool lastMotionState = false;

char buffer[64];
byte bufferIndex = 0;

// ======================================================
// FORWARD DECLARATIONS
// ======================================================

void ReadSerial();
void ProcessCommand(const char* cmd);

void UpdateStateMachine();
void UpdateLED();
void UpdateMotionPulseCounter();

void StartOpen();
void StartClose();

void PulseTriggerRelay();

void StopAll();
void StopAllRelays();

void SendStatus();

void Ack(const char* cmd);
void Nack(const char* cmd);

bool IsSafe();
bool IsOpen();
bool IsClosed();
bool IsMotionDetected();

// ======================================================
// SENSOR FUNCTIONS
// ======================================================

bool IsSafe()
{
  if (!safeModeEnabled)
    return true;

  return digitalRead(PIN_SAFE) == SAFE_ACTIVE;
}

bool IsOpen()
{
  return digitalRead(PIN_OPENED) == OPEN_ACTIVE;
}

bool IsClosed()
{
  return digitalRead(PIN_CLOSED) == CLOSE_ACTIVE;
}

bool IsMotionDetected()
{
  if (!motionSensorEnabled)
    return true;

  return digitalRead(PIN_MOTION) == MOTION_ACTIVE;
}

// ======================================================
// MOTION PULSE TRACKING
// ======================================================

void UpdateMotionPulseCounter()
{
  if (!motionSensorEnabled)
    return;

  bool currentState =
    (digitalRead(PIN_MOTION) == MOTION_ACTIVE);

  // Edge detection
  if (currentState && !lastMotionState)
  {
    motionPulseCount++;
    lastMotionTime = millis();
  }

  lastMotionState = currentState;
}

// ======================================================
// SETUP
// ======================================================

void setup()
{
  // Configure relay outputs
  pinMode(RELAY_TRIGGER, OUTPUT);
  pinMode(RELAY_UNUSED, OUTPUT);
  pinMode(RELAY_SENSOR, OUTPUT);
  pinMode(RELAY_SPARE, OUTPUT);

  StopAllRelays();

  // Configure inputs
  pinMode(PIN_OPENED, INPUT_PULLUP);
  pinMode(PIN_CLOSED, INPUT_PULLUP);
  pinMode(PIN_SAFE, INPUT_PULLUP);
  pinMode(PIN_MOTION, INPUT_PULLUP);

  // Configure LED
  pinMode(LED_PIN, OUTPUT);
  digitalWrite(LED_PIN, LOW);

  delay(500);

  // Start serial
  Serial.begin(9600);

  delay(100);

  // Clear serial buffer
  while (Serial.available() > 0)
    Serial.read();

  bufferIndex = 0;
  lastSerialByteTime = 0;

  // Initial roof state
  if (IsOpen())
    state = OPEN;
  else if (IsClosed())
    state = CLOSED;
  else
    state = IDLE;
}

// ======================================================
// MAIN LOOP
// ======================================================

void loop()
{
  ReadSerial();

  UpdateMotionPulseCounter();

  UpdateStateMachine();

  UpdateLED();
}

// ======================================================
// SERIAL INPUT
// ======================================================

void ReadSerial()
{
  // Recover from partial command
  if (bufferIndex > 0 &&
      (millis() - lastSerialByteTime >
       SERIAL_COMMAND_TIMEOUT))
  {
    bufferIndex = 0;
  }

  while (Serial.available() > 0)
  {
    char c = Serial.read();

    lastSerialByteTime = millis();

    if (c == '#')
    {
      buffer[bufferIndex] = '\0';

      ProcessCommand(buffer);

      bufferIndex = 0;
    }
    else if (c >= 32 && c <= 126)
    {
      if (bufferIndex < sizeof(buffer) - 1)
      {
        buffer[bufferIndex++] = c;
      }
      else
      {
        // Overflow protection
        bufferIndex = 0;
      }
    }
  }
}

// ======================================================
// COMMAND PROCESSING
// ======================================================

void ProcessCommand(const char* cmd)
{
  // --------------------------------------------------
  // Ping
  // --------------------------------------------------

  if (strcmp(cmd, "ping") == 0)
  {
    Serial.print("PONG#");
    Serial.flush();
    return;
  }

  // --------------------------------------------------
  // Status
  // --------------------------------------------------

  if (strcmp(cmd, "status") == 0)
  {
    SendStatus();
    return;
  }

    // --------------------------------------------------
  // Pulse Count
  // --------------------------------------------------

  if (strcmp(cmd, "getpulsecount") == 0)
  {
    Serial.print("PULSES:");
    Serial.print(motionPulseCount);
    Serial.print('#');
    Serial.flush();
    return;
  }

  // --------------------------------------------------
  // Reset Pulse Count
  // --------------------------------------------------

  if (strcmp(cmd, "resetpulse") == 0)
  {
    motionPulseCount = 0;

    Serial.print("OK#");

    Serial.flush();

    return;
  }

  // --------------------------------------------------
  // Open
  // --------------------------------------------------
  
  if (strcmp(cmd, "open") == 0)
  {
    StartOpen();
    Ack("open");
    return;
  }

  // --------------------------------------------------
  // Close
  // --------------------------------------------------

  if (strcmp(cmd, "close") == 0)
  {
    StartClose();
    Ack("close");
    return;
  }

  // --------------------------------------------------
  // Abort
  // --------------------------------------------------

  if (strcmp(cmd, "abort") == 0)
  {
    StopAll();
    Ack("abort");
    return;
  }

  // --------------------------------------------------
  // Safe Mode
  // --------------------------------------------------

  if (strcmp(cmd, "setsafe:1") == 0)
  {
    safeModeEnabled = true;
    Ack("setsafe");
    return;
  }

  if (strcmp(cmd, "setsafe:0") == 0)
  {
    safeModeEnabled = false;
    Ack("setsafe");
    return;
  }

  // --------------------------------------------------
  // Motion Sensor
  // --------------------------------------------------

  if (strcmp(cmd, "setmotion:1") == 0)
  {
    motionSensorEnabled = true;
    Ack("setmotion");
    return;
  }

  if (strcmp(cmd, "setmotion:0") == 0)
  {
    motionSensorEnabled = false;
    Ack("setmotion");
    return;
  }

  // --------------------------------------------------
  // Unknown Command
  // --------------------------------------------------

  Nack(cmd);
}

// ======================================================
// STATE MACHINE
// ======================================================

void UpdateStateMachine()
{
  unsigned long now = millis();

  // --------------------------------------------------
  // Open Sensor
  // --------------------------------------------------

  if (IsOpen())
  {
    state = OPEN;
    return;
  }

  // --------------------------------------------------
  // Closed Sensor
  // --------------------------------------------------

  if (IsClosed())
  {
    state = CLOSED;
    return;
  }

  // --------------------------------------------------
  // Motion Monitoring
  // --------------------------------------------------

  if (state == OPENING || state == CLOSING)
  {
    // Hard timeout
    if (now - moveStart > MOVE_TIMEOUT)
    {
      state = ERROR;
      return;
    }

    // Motion timeout
    if (motionSensorEnabled)
    {
      if (now - lastMotionTime >
          MOTION_CHECK_INTERVAL)
      {
        state = ERROR;
        return;
      }
    }
  }

  // --------------------------------------------------
  // Idle state cleanup
  // --------------------------------------------------

  if (state == OPEN || state == CLOSED)
  {
    state = IDLE;
  }
}

// ======================================================
// OPEN / CLOSE
// ======================================================

void StartOpen()
{
  if (!IsSafe())
  {
    state = ERROR;
    return;
  }

  if (IsOpen())
  {
    state = OPEN;
    return;
  }

  state = OPENING;

  moveStart = millis();

  lastMotionTime = moveStart;

  motionPulseCount = 0;

  lastMotionState = false;

  PulseTriggerRelay();
}

void StartClose()
{
  if (!IsSafe())
  {
    state = ERROR;
    return;
  }

  if (IsClosed())
  {
    state = CLOSED;
    return;
  }

  state = CLOSING;

  moveStart = millis();

  lastMotionTime = moveStart;

  motionPulseCount = 0;

  lastMotionState = false;

  PulseTriggerRelay();
}

// ======================================================
// RELAY PULSE
// ======================================================

void PulseTriggerRelay()
{
  digitalWrite(RELAY_TRIGGER, LOW);

  delay(100);

  digitalWrite(RELAY_TRIGGER, HIGH);

  delay(RELAY_PULSE_TIME);

  digitalWrite(RELAY_TRIGGER, LOW);
}

// ======================================================
// STOP
// ======================================================

void StopAll()
{
  StopAllRelays();

  lastMotionState = false;

  if (IsOpen())
    state = OPEN;
  else if (IsClosed())
    state = CLOSED;
  else
    state = IDLE;
}

void StopAllRelays()
{
  digitalWrite(RELAY_TRIGGER, LOW);
  digitalWrite(RELAY_UNUSED, LOW);
  digitalWrite(RELAY_SENSOR, LOW);
  digitalWrite(RELAY_SPARE, LOW);
}

// ======================================================
// STATUS
// ======================================================

void SendStatus()
{
  Serial.print("STATE:");

  if (IsOpen())
  {
    Serial.print("OPEN;");
  }
  else if (IsClosed())
  {
    Serial.print("CLOSED;");
  }
  else
  {
    switch (state)
    {
      case OPENING:
        Serial.print("OPENING;");
        break;

      case CLOSING:
        Serial.print("CLOSING;");
        break;

      case ERROR:
        Serial.print("ERROR;");
        break;

      default:
        Serial.print("IDLE;");
        break;
    }
  }

  Serial.print(IsSafe() ?
               "SAFE;" :
               "UNSAFE;");

  if (motionSensorEnabled)
  {
    Serial.print("PULSES:");
    Serial.print(motionPulseCount);
    Serial.print(';');
  }

  if (state == OPENING ||
      state == CLOSING)
  {
    Serial.print("MOVING#");
  }
  else
  {
    Serial.print("IDLE#");
  }

  Serial.flush();
}

// ======================================================
// LED STATUS
// ======================================================

void UpdateLED()
{
  if (state == OPENING ||
      state == CLOSING)
  {
    digitalWrite(
      LED_PIN,
      (millis() / 250) % 2);

    return;
  }

  if (state == ERROR)
  {
    digitalWrite(
      LED_PIN,
      (millis() / 100) % 2);

    return;
  }

  digitalWrite(
    LED_PIN,
    IsSafe() ? HIGH : LOW);
}

// ======================================================
// ACK / NACK
// ======================================================

void Ack(const char* cmd)
{
  Serial.print("OK:");
  Serial.print(cmd);
  Serial.print('#');

  Serial.flush();
}

void Nack(const char* cmd)
{
  Serial.print("ERR:");
  Serial.print(cmd);
  Serial.print('#');

  Serial.flush();
}
