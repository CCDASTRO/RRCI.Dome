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
// - ASCOM-compatible serial protocol
// - Robust serial command parser with reconnect recovery
// ======================================================

#include <string.h>

// ======================================================
// PINS
// ======================================================
#define PIN_OPENED        11
#define PIN_CLOSED        12
#define PIN_SAFE          13
#define PIN_MOTION         2   // Hall sensor input

#define RELAY_TRIGGER      7
#define RELAY_UNUSED       6
#define RELAY_SENSOR       5
#define RELAY_SPARE        4

#define LED_PIN           10

// ======================================================
// SENSOR POLARITY
// ======================================================
#define OPEN_ACTIVE      LOW
#define CLOSE_ACTIVE     LOW
#define SAFE_ACTIVE      LOW
#define MOTION_ACTIVE    LOW

// ======================================================
// TIMING
// ======================================================
const unsigned long MOVE_TIMEOUT          = 60000UL;
const unsigned long RELAY_PULSE_TIME      = 500UL;
const unsigned long MOTION_CHECK_INTERVAL = 3000UL;
const unsigned long SERIAL_COMMAND_TIMEOUT = 1000UL; // Recover from partial commands

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
unsigned long motionPulseCount = 0;
unsigned long lastSerialByteTime = 0;

bool safeModeEnabled = false;
bool motionSensorEnabled = false;

char buffer[64];
byte bufferIndex = 0;

// ======================================================
// FORWARD DECLARATIONS
// ======================================================
void ReadSerial();
void ProcessCommand(const char* cmd);
void UpdateStateMachine();
void UpdateLED();
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

  // Configure status LED
  pinMode(LED_PIN, OUTPUT);
  digitalWrite(LED_PIN, LOW);

  // Allow hardware to settle
  delay(500);

  // Start serial
  Serial.begin(9600);

  // Brief pause only
  delay(100);

  // Clear any bytes already in the buffer
  while (Serial.available() > 0)
    Serial.read();

  // Reset command parser
  bufferIndex = 0;
  lastSerialByteTime = 0;

  // Determine initial roof state
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
  UpdateStateMachine();
  UpdateLED();
}

// ======================================================
// SERIAL INPUT
// ======================================================
void ReadSerial()
{
  // If command reception stalls, discard partial command.
  if (bufferIndex > 0 &&
      (millis() - lastSerialByteTime > SERIAL_COMMAND_TIMEOUT))
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
    else if (c >= 32 && c <= 126) // printable ASCII only
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
  if (strcmp(cmd, "ping") == 0)
  {
    Serial.print("PONG#");
    Serial.flush();
    return;
  }

  if (strcmp(cmd, "status") == 0)
  {
    SendStatus();
    return;
  }

  if (strcmp(cmd, "open") == 0)
  {
    StartOpen();
    Ack("open");
    return;
  }

  if (strcmp(cmd, "close") == 0)
  {
    StartClose();
    Ack("close");
    return;
  }

  if (strcmp(cmd, "abort") == 0)
  {
    StopAll();
    Ack("abort");
    return;
  }

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

  Nack(cmd);
}

// ======================================================
// STATE MACHINE
// ======================================================
void UpdateStateMachine()
{
  if (IsOpen())
  {
    state = OPEN;
    return;
  }

  if (IsClosed())
  {
    state = CLOSED;
    return;
  }

  if (state == OPENING || state == CLOSING)
  {
    unsigned long now = millis();

    if (now - moveStart > MOVE_TIMEOUT)
    {
      state = ERROR;
      return;
    }

    if (motionSensorEnabled)
    {
      if (IsMotionDetected())
      {
        motionPulseCount++;
        lastMotionTime = now;
        delay(10); // debounce
      }

      if (now - lastMotionTime > MOTION_CHECK_INTERVAL)
      {
        state = ERROR;
        return;
      }
    }
  }

  if (state == OPEN || state == CLOSED)
    state = IDLE;
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

  moveStart = millis();
  lastMotionTime = moveStart;
  motionPulseCount = 0;
}

// ======================================================
// STOP
// ======================================================
void StopAll()
{
  StopAllRelays();

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
    Serial.print("OPEN;");
  else if (IsClosed())
    Serial.print("CLOSED;");
  else
  {
    switch (state)
    {
      case OPENING: Serial.print("OPENING;"); break;
      case CLOSING: Serial.print("CLOSING;"); break;
      case ERROR:   Serial.print("ERROR;"); break;
      default:      Serial.print("IDLE;"); break;
    }
  }

  Serial.print(IsSafe() ? "SAFE;" : "UNSAFE;");

  if (motionSensorEnabled)
  {
    Serial.print("PULSES:");
    Serial.print(motionPulseCount);
    Serial.print(';');
  }

  Serial.print((state == OPENING || state == CLOSING) ? "MOVING#" : "IDLE#");
  Serial.flush();
}

// ======================================================
// LED STATUS
// ======================================================
void UpdateLED()
{
  if (state == OPENING || state == CLOSING)
  {
    digitalWrite(LED_PIN, (millis() / 250) % 2);
    return;
  }

  if (state == ERROR)
  {
    digitalWrite(LED_PIN, (millis() / 100) % 2);
    return;
  }

  digitalWrite(LED_PIN, IsSafe() ? HIGH : LOW);
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
