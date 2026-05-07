// ======================================================
// ASCOM Roll-Off Roof Controller Firmware
// Single Relay Toggle Version
// ACTIVE HIGH RELAY VERSION
//
// Chuck Faranda - https://ccdastro.net
//
// Designed for:
// - ASCOM RRCI Dome Driver
// - NINA / Voyager / SGP compatibility
//
// STABLE VERSION
//
// FEATURES
// ------------------------------------------------------
// - Safe relay startup
// - No relay activation on boot/upload
// - Single momentary trigger relay
// - ACTIVE HIGH relay board support
// - Live sensor state reporting
// - Reliable OPEN/CLOSED detection
// - OPENING/CLOSING software states
// - Manual switch test compatible
// - Clean ASCOM serial protocol
// - No blocking state logic
// - No false ERROR states
//
// USE ONLY WITH:
//
// Garage-door style roof controller:
//
// Pulse = Open -> Stop -> Close -> Stop
// ======================================================

#include <string.h>


// ======================================================
// PINS
// ======================================================

#define PIN_OPENED       11
#define PIN_CLOSED       12
#define PIN_SAFE         13

#define RELAY_TRIGGER     7
#define RELAY_UNUSED      6
#define RELAY_SENSOR      5
#define RELAY_SPARE       4

#define LED_PIN          10


// ======================================================
// SENSOR POLARITY
// ======================================================

#define OPEN_ACTIVE     LOW
#define CLOSE_ACTIVE    LOW
#define SAFE_ACTIVE     LOW


// ======================================================
// TIMING
// ======================================================

const unsigned long MOVE_TIMEOUT      = 60000;
const unsigned long RELAY_PULSE_TIME  = 500;


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

char buffer[32];
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


// ======================================================
// SENSOR FUNCTIONS
// ======================================================

// TEMPORARY SAFE BYPASS
// ENABLE REAL SAFE INPUT LATER

bool IsSafe()
{
  return true;

  // ENABLE LATER:
  // return digitalRead(PIN_SAFE) == SAFE_ACTIVE;
}

bool IsOpen()
{
  return digitalRead(PIN_OPENED) == OPEN_ACTIVE;
}

bool IsClosed()
{
  return digitalRead(PIN_CLOSED) == CLOSE_ACTIVE;
}


// ======================================================
// SETUP
// ======================================================

void setup()
{
  // --------------------------------------------------
  // Configure relay outputs FIRST
  // Prevent relay glitch during Arduino boot
  // --------------------------------------------------

  pinMode(RELAY_TRIGGER, OUTPUT);
  pinMode(RELAY_UNUSED, OUTPUT);
  pinMode(RELAY_SENSOR, OUTPUT);
  pinMode(RELAY_SPARE, OUTPUT);

  // ACTIVE HIGH BOARD:
  // LOW = OFF

  digitalWrite(RELAY_TRIGGER, LOW);
  digitalWrite(RELAY_UNUSED, LOW);
  digitalWrite(RELAY_SENSOR, LOW);
  digitalWrite(RELAY_SPARE, LOW);

  // --------------------------------------------------
  // Inputs
  // --------------------------------------------------

  pinMode(PIN_OPENED, INPUT_PULLUP);
  pinMode(PIN_CLOSED, INPUT_PULLUP);
  pinMode(PIN_SAFE, INPUT_PULLUP);

  // --------------------------------------------------
  // LED
  // --------------------------------------------------

  pinMode(LED_PIN, OUTPUT);

  digitalWrite(LED_PIN, LOW);

  // --------------------------------------------------
  // Allow hardware to stabilize
  // --------------------------------------------------

  delay(1000);

  // --------------------------------------------------
  // Serial
  // --------------------------------------------------

  Serial.begin(9600);

  // --------------------------------------------------
  // Ensure relays OFF
  // --------------------------------------------------

  StopAllRelays();

  // --------------------------------------------------
  // Initial state from sensors
  // --------------------------------------------------

  if (IsOpen())
  {
    state = OPEN;
  }
  else if (IsClosed())
  {
    state = CLOSED;
  }
  else
  {
    state = IDLE;
  }
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
  while (Serial.available())
  {
    char c = Serial.read();

    if (c == '#')
    {
      buffer[bufferIndex] = '\0';

      ProcessCommand(buffer);

      bufferIndex = 0;
    }
    else
    {
      if (bufferIndex < sizeof(buffer) - 1)
      {
        buffer[bufferIndex++] = c;
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
  // Unknown
  // --------------------------------------------------

  Nack(cmd);
}


// ======================================================
// STATE MACHINE
// ======================================================

void UpdateStateMachine()
{
  // --------------------------------------------------
  // LIVE SENSOR PRIORITY
  // --------------------------------------------------

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

  // --------------------------------------------------
  // Between sensors
  // --------------------------------------------------

  switch (state)
  {
    case OPEN:
    case CLOSED:

      state = IDLE;
      break;

    case OPENING:

      if (millis() - moveStart > MOVE_TIMEOUT)
      {
        state = ERROR;
      }

      break;

    case CLOSING:

      if (millis() - moveStart > MOVE_TIMEOUT)
      {
        state = ERROR;
      }

      break;

    default:
      break;
  }
}


// ======================================================
// OPEN COMMAND
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


// ======================================================
// CLOSE COMMAND
// ======================================================

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
  // Ensure OFF first

  digitalWrite(RELAY_TRIGGER, LOW);

  delay(100);

  // ACTIVE HIGH pulse

  digitalWrite(RELAY_TRIGGER, HIGH);

  delay(RELAY_PULSE_TIME);

  // OFF again

  digitalWrite(RELAY_TRIGGER, LOW);

  moveStart = millis();
}


// ======================================================
// STOP
// ======================================================

void StopAll()
{
  StopAllRelays();

  if (IsOpen())
  {
    state = OPEN;
  }
  else if (IsClosed())
  {
    state = CLOSED;
  }
  else
  {
    state = IDLE;
  }
}


// ======================================================
// RELAYS
// ======================================================

void StopAllRelays()
{
  // ACTIVE HIGH:
  // LOW = OFF

  digitalWrite(RELAY_TRIGGER, LOW);
  digitalWrite(RELAY_UNUSED, LOW);
  digitalWrite(RELAY_SENSOR, LOW);
  digitalWrite(RELAY_SPARE, LOW);
}


// ======================================================
// STATUS REPORTING
// ======================================================

void SendStatus()
{
  Serial.print("STATE:");

  // --------------------------------------------------
  // Sensors ALWAYS win
  // --------------------------------------------------

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

  // --------------------------------------------------
  // Safety
  // --------------------------------------------------

  if (IsSafe())
  {
    Serial.print("SAFE;");
  }
  else
  {
    Serial.print("UNSAFE;");
  }

  // --------------------------------------------------
  // Motion
  // --------------------------------------------------

  if (state == OPENING || state == CLOSING)
  {
    Serial.print("MOVING#");
  }
  else
  {
    Serial.print("IDLE#");
  }
}


// ======================================================
// LED STATUS
// ======================================================

void UpdateLED()
{
  // MOVING

  if (state == OPENING || state == CLOSING)
  {
    digitalWrite(LED_PIN, (millis() / 250) % 2);
    return;
  }

  // ERROR

  if (state == ERROR)
  {
    digitalWrite(LED_PIN, (millis() / 100) % 2);
    return;
  }

  // SAFE / IDLE

  digitalWrite(LED_PIN, IsSafe() ? HIGH : LOW);
}


// ======================================================
// ACK
// ======================================================

void Ack(const char* cmd)
{
  Serial.print("OK:");
  Serial.print(cmd);
  Serial.print("#");
}


// ======================================================
// NACK
// ======================================================

void Nack(const char* cmd)
{
  Serial.print("ERR:");
  Serial.print(cmd);
  Serial.print("#");
}
