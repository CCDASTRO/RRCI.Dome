// ======================================================
// ASCOM-Grade Roll-Off Roof Controller Firmware
// FIXED: deterministic serial + safe handshake
// ======================================================

// ---------------- PINS ----------------
#define PIN_OPENED  11
#define PIN_CLOSED  12
#define PIN_SAFE    13

#define RELAY_OPEN   7
#define RELAY_CLOSE  6
#define RELAY_SENSOR 5
#define RELAY_SPARE  4
#define LED_PIN      10

// ---------------- STATES ----------------
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

// ---------------- TIMERS ----------------
unsigned long moveStart = 0;
unsigned long lastHeartbeat = 0;
unsigned long lastStatus = 0;

const unsigned long MOVE_TIMEOUT = 60000;
const unsigned long HEARTBEAT_TIMEOUT = 120000;
const unsigned long STATUS_INTERVAL = 1000;

// ---------------- SERIAL BUFFER ----------------
char buf[32];
byte idx = 0;

// ---------------- FIX: handshake lock ----------------
bool handshakeMode = false;

// ---------------- HELPERS ----------------
#define SAFE_ACTIVE LOW
#define OPEN_ACTIVE LOW
#define CLOSE_ACTIVE LOW

bool isSafe()   { return digitalRead(PIN_SAFE) == SAFE_ACTIVE; }
bool isOpen()   { return digitalRead(PIN_OPENED) == OPEN_ACTIVE; }
bool isClosed() { return digitalRead(PIN_CLOSED) == CLOSE_ACTIVE; }

// ---------------- SETUP ----------------
void setup()
{
  Serial.begin(9600);

  pinMode(PIN_OPENED, INPUT_PULLUP);
  pinMode(PIN_CLOSED, INPUT_PULLUP);
  pinMode(PIN_SAFE, INPUT_PULLUP);

  pinMode(RELAY_OPEN, OUTPUT);
  pinMode(RELAY_CLOSE, OUTPUT);
  pinMode(RELAY_SENSOR, OUTPUT);
  pinMode(RELAY_SPARE, OUTPUT);
  pinMode(LED_PIN, OUTPUT);

  stopAllRelays();

  state = IDLE;

  // clean startup banner (safe)
  Serial.print("RRCI#");
}

// ---------------- LOOP ----------------
void loop()
{
  readSerial();
  updateStateMachine();
  updateHeartbeat();
  updateLED();
  periodicStatus();
}

// ======================================================
// SERIAL HANDLING
// ======================================================
void readSerial()
{
  while (Serial.available())
  {
    char c = Serial.read();

    if (c == '#')
    {
      buf[idx] = '\0';
      processCommand(buf);
      idx = 0;
    }
    else if (idx < sizeof(buf) - 1)
    {
      buf[idx++] = c;
    }
  }
}

// ---------------- COMMAND PROCESSOR ----------------
void processCommand(const char* cmd)
{
  lastHeartbeat = millis();

  if (strcmp(cmd, "ping") == 0)
  {
    // FIX: isolate handshake from status spam
    handshakeMode = true;

    Serial.print("PONG#");

    handshakeMode = false;
    return;
  }

  if (strcmp(cmd, "status") == 0)
  {
    sendStatus();
    return;
  }

  if (strcmp(cmd, "open") == 0)
  {
    startOpen();
    ack("open");
    return;
  }

  if (strcmp(cmd, "close") == 0)
  {
    startClose();
    ack("close");
    return;
  }

  if (strcmp(cmd, "abort") == 0)
  {
    stopAll();
    ack("abort");
    return;
  }

  nack(cmd);
}

// ======================================================
// STATE MACHINE
// ======================================================
void updateStateMachine()
{
  switch (state)
  {
    case OPENING:
      if (isOpen())
      {
        stopAllRelays();
        state = OPEN;
      }
      else if (millis() - moveStart > MOVE_TIMEOUT)
      {
        state = ERROR;
        stopAllRelays();
      }
      break;

    case CLOSING:
      if (isClosed())
      {
        stopAllRelays();
        state = CLOSED;
      }
      else if (millis() - moveStart > MOVE_TIMEOUT)
      {
        state = ERROR;
        stopAllRelays();
      }
      break;

    default:
      break;
  }
}

// ======================================================
// ACTIONS
// ======================================================
void startOpen()
{
  if (!isSafe()) return;

  stopAllRelays();

  digitalWrite(RELAY_SENSOR, HIGH);
  delay(200);

  digitalWrite(RELAY_OPEN, HIGH);

  state = OPENING;
  moveStart = millis();
}

void startClose()
{
  if (!isSafe()) return;

  stopAllRelays();

  digitalWrite(RELAY_SENSOR, HIGH);
  delay(200);

  digitalWrite(RELAY_CLOSE, HIGH);

  state = CLOSING;
  moveStart = millis();
}

void stopAll()
{
  stopAllRelays();
  state = IDLE;
}

// ======================================================
// RELAYS
// ======================================================
void stopAllRelays()
{
  digitalWrite(RELAY_OPEN, LOW);
  digitalWrite(RELAY_CLOSE, LOW);
  digitalWrite(RELAY_SENSOR, LOW);
  digitalWrite(RELAY_SPARE, LOW);
}

// ======================================================
// STATUS OUTPUT (FIXED SAFE MODE)
// ======================================================
void sendStatus()
{
  if (handshakeMode) return; // prevent collisions during connect

  Serial.print("STATE:");

  switch (state)
  {
    case OPEN:    Serial.print("OPEN;"); break;
    case CLOSED:  Serial.print("CLOSED;"); break;
    case OPENING: Serial.print("OPENING;"); break;
    case CLOSING: Serial.print("CLOSING;"); break;
    case ERROR:   Serial.print("ERROR;"); break;
    default:      Serial.print("IDLE;"); break;
  }

  if (isSafe()) Serial.print("SAFE;");
  else Serial.print("UNSAFE;");

  if (state == OPENING || state == CLOSING)
    Serial.print("MOVING#");
  else
    Serial.print("IDLE#");
}

// ======================================================
// HEARTBEAT SAFETY
// ======================================================
void updateHeartbeat()
{
  if (millis() - lastHeartbeat > HEARTBEAT_TIMEOUT)
  {
    stopAll();
    state = ERROR;
  }
}

// ======================================================
// LED STATUS
// ======================================================
void updateLED()
{
  digitalWrite(LED_PIN, isSafe() ? HIGH : LOW);
}

// ======================================================
// PERIODIC STATUS (SAFE FROM HANDSHAKE COLLISION)
// ======================================================
void periodicStatus()
{
  if (handshakeMode) return;

  if (millis() - lastStatus > STATUS_INTERVAL)
  {
    lastStatus = millis();
    sendStatus();
  }
}

// ======================================================
// ACK / NACK
// ======================================================
void ack(const char* cmd)
{
  Serial.print("OK:");
  Serial.print(cmd);
  Serial.print("#");
}

void nack(const char* cmd)
{
  Serial.print("ERR:");
  Serial.print(cmd);
  Serial.print("#");
}
