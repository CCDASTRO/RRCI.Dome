// ======================================================
// ASCOM Roll-Off Roof Controller Firmware
// Single-Relay Toggle Version (Garage Door Style)
// Chuck Faranda - https://ccdastro.net
//
// Designed for:
// - ASCOM RRCI Dome Driver
// - NINA / SGP / Voyager compatibility
//
// USE ONLY if your roof controller uses ONE momentary
// trigger input like a garage door opener:
//
// Pulse = Open -> Stop -> Close -> Stop
//
// IMPORTANT:
// This is less safe than dedicated OPEN/CLOSE relays.
// Limit switches are REQUIRED.
// ======================================================


// ======================================================
// PINS
// ======================================================

#define PIN_OPENED     11
#define PIN_CLOSED     12
#define PIN_SAFE       13

#define RELAY_TRIGGER   7   // single trigger relay
#define RELAY_UNUSED    6
#define RELAY_SENSOR    5
#define RELAY_SPARE     4

#define LED_PIN        10


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
// TIMERS
// ======================================================

unsigned long moveStart = 0;
unsigned long lastHeartbeat = 0;

const unsigned long MOVE_TIMEOUT = 60000;         // 60 sec
const unsigned long HEARTBEAT_TIMEOUT = 120000;   // 2 min
const unsigned long RELAY_PULSE_TIME = 1000;      // 1 sec button press


// ======================================================
// SERIAL BUFFER
// ======================================================

char buffer[32];
byte bufferIndex = 0;


// ======================================================
// SENSOR LOGIC
// ======================================================

#define SAFE_ACTIVE     LOW
#define OPEN_ACTIVE     LOW
#define CLOSE_ACTIVE    LOW

bool IsSafe()
{
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


// ======================================================
// SETUP
// ======================================================

void setup()
{
  Serial.begin(9600);

  pinMode(PIN_OPENED, INPUT_PULLUP);
  pinMode(PIN_CLOSED, INPUT_PULLUP);
  pinMode(PIN_SAFE, INPUT_PULLUP);

  pinMode(RELAY_TRIGGER, OUTPUT);
  pinMode(RELAY_UNUSED, OUTPUT);
  pinMode(RELAY_SENSOR, OUTPUT);
  pinMode(RELAY_SPARE, OUTPUT);
  pinMode(LED_PIN, OUTPUT);

  StopAllRelays();

  // No startup serial output
  // ASCOM requires clean connect

  if (IsClosed())
    state = CLOSED;
  else if (IsOpen())
    state = OPEN;
  else
    state = IDLE;

  lastHeartbeat = millis();
}


// ======================================================
// MAIN LOOP
// ======================================================

void loop()
{
  ReadSerial();
  UpdateStateMachine();
  UpdateHeartbeatSafety();
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
// COMMAND PROCESSOR
// ======================================================

void ProcessCommand(const char* cmd)
{
  lastHeartbeat = millis();

  if (strcmp(cmd, "ping") == 0)
  {
    Serial.print("PONG#");
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

  Nack(cmd);
}


// ======================================================
// STATE MACHINE
// ======================================================

void UpdateStateMachine()
{
  switch (state)
  {
    case OPENING:

      if (IsOpen())
      {
        StopAllRelays();
        state = OPEN;
      }
      else if (millis() - moveStart > MOVE_TIMEOUT)
      {
        StopAllRelays();
        state = ERROR;
      }

      break;

    case CLOSING:

      if (IsClosed())
      {
        StopAllRelays();
        state = CLOSED;
      }
      else if (millis() - moveStart > MOVE_TIMEOUT)
      {
        StopAllRelays();
        state = ERROR;
      }

      break;

    default:
      break;
  }
}


// ======================================================
// RELAY PULSE ACTION
// ======================================================

void PulseTriggerRelay()
{
  StopAllRelays();

  // Optional interlock relay enable
  digitalWrite(RELAY_SENSOR, HIGH);
  delay(200);

  // Simulate garage door button press
  digitalWrite(RELAY_TRIGGER, HIGH);
  delay(RELAY_PULSE_TIME);
  digitalWrite(RELAY_TRIGGER, LOW);

  moveStart = millis();
}


// ======================================================
// ACTIONS
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

  PulseTriggerRelay();
  state = OPENING;
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

  PulseTriggerRelay();
  state = CLOSING;
}


void StopAll()
{
  StopAllRelays();

  if (IsClosed())
    state = CLOSED;
  else if (IsOpen())
    state = OPEN;
  else
    state = IDLE;
}


// ======================================================
// RELAYS
// ======================================================

void StopAllRelays()
{
  digitalWrite(RELAY_TRIGGER, LOW);
  digitalWrite(RELAY_UNUSED, LOW);
  digitalWrite(RELAY_SENSOR, LOW);
  digitalWrite(RELAY_SPARE, LOW);
}


// ======================================================
// STATUS RESPONSE
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

  if (IsSafe())
    Serial.print("SAFE;");
  else
    Serial.print("UNSAFE;");

  if (state == OPENING || state == CLOSING)
    Serial.print("MOVING#");
  else
    Serial.print("IDLE#");
}


// ======================================================
// HEARTBEAT FAILSAFE
// ======================================================

void UpdateHeartbeatSafety()
{
  if (millis() - lastHeartbeat > HEARTBEAT_TIMEOUT)
  {
    StopAllRelays();
    state = ERROR;
  }
}


// ======================================================
// LED STATUS
// ======================================================

void UpdateLED()
{
  digitalWrite(LED_PIN, IsSafe() ? HIGH : LOW);
}


// ======================================================
// ACK / NACK
// ======================================================

void Ack(const char* cmd)
{
  Serial.print("OK:");
  Serial.print(cmd);
  Serial.print("#");
}

void Nack(const char* cmd)
{
  Serial.print("ERR:");
  Serial.print(cmd);
  Serial.print("#");
}
