// ======================================================
// ASCOM Roll-Off Roof Controller Firmware
// Multi Relay Toggle Version
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

/* =======================================================
Summary of changes I have made to this sketch
-note that I am using a true Arduino Relay board, the pins that control the relays are different from other 3rd party relay boards

Corrected repository variant:
- Preserves the user's custom relay and sensor pin assignments.
- Preserves the gable-fan and operator-power relay behavior.
- Uses a 60-second production movement timeout.
- Keeps OPENING/CLOSING active while leaving the starting limit.
- Completes travel only at the destination limit.
- Reports conflicting limits, motion loss, and travel timeout as ERROR.
- Uses no blocking fault loop or repeated startup delay.

The testing narrative below documents the original user investigation. The
ROOF_BEGIN, stuck-loop, and malformed status experiments described there were
removed from this corrected variant.

    Relay Configuration
-RELAY_SPARE (K1) is pin 4, was relabeled as RELAY _FAN and repurposed to control the gable roof fan
-RELAY_OPERATOR (K2), pin 7, I have added this relay and associated code to control the operator power.
-RELAY_OPEN (K3) was pin 7, I have changed it to pin 8 (using as O/C/S)
-RELAY_STOP (K4) was pin 5, I have changed to pin 12 to match my wiring
-RELAY_CLOSE was pin 6, I will not use this, but I will leave the related code in to preserve original code.

    Sensor Configuration
-PIN_MOTION remains pin 2
-LED_PIN was pin 10, I have changed it to pin 9 to match my wiring
-PIN_OPENED was pin 11, I have changed it to pin 10 to match my wiring
-PIN_CLOSED was pin 12, I have changed it to pin 11 to match my wiring
-PIN_SAFE remains pin 13

==================================================

==================================================
  Notes about behavior and issue discovery during testing using a breadboard with Arduino, Manual switches and LED's simulating the relays
  
  TESTING RESULTS with "1 button controller" and "scope Safe" selected, and Roof Motion Sensor NOT Selected.
  -----------------------------------
  SAFE LED behavior;
  -LED is lit as soon as the Arduino has power, until the driver is connected regardless of the status of the safety switch(s).(ISSUE1) 
  - Once the Driver is connected via NINA the LED behaves as follows;
    -LED is not lit if safety switch(s) are open (PIN_SAFE == HIGH)
    -LED blinks during roof movement
    -LED blinks faster when RoofState State == ERROR. The RoofState and the LED return to normal if the source of the ERROR is eliminated.

    I changed safeModeEnabled default setting to true to resolve ISSUE1, the LED now follows PIN_SAFE status, except that it blinks off
      once as driver connects, if safe switch is connected and closed, if nothing is connected to PINSAFE, the LED remains off

  LIMIT SWITCH behavior;
  -Normal operation from NINA is as expected;
    -Roof being full OPEN or CLOSED reports OPEN and CLOSED respectivly on RRCI interface and in NINA
    -Roof Moving normally from OPEN or CLOSED
      -reports OPENING and CLOSING on RRCI and in NINA
      -reports Closed or Open when movement is complete on both RRCI and NINA.

    -If the appropiate limit does not open almost immediatly (simulating that the roof may be a little slow to get moving and open
     open the limit switch before the IsOpen or IsClosed routine runs and changes the state from OPENING or CLOSING back to 
     CLOSED or OPEN)
      -RRCI and NINA status remain Open or Closed
      -The LED status reverts back to SAFE status (ISSUE2)

      I added the constant ROOF_BEGIN to allow the roof time to start moving before updating state or LED status(still trying to resove this)

    -If the appropriate limit does not open at all (simulating that roof movement failed to start);
      -RRCI and NINA both report Opening or Closing respectively, this last for about 2 minutes, then NINA displays a non-conformant WARNING
      on screen and both NINA and RRCI change to Open or Closed respectivly. (ISSUE3)
      -NO ERROR state is reported by RRCI (ISSUE3)

      I added code under the MachineState function to Send ERROR state, update LED to flash quickly, Turn off my operator relay, 
       then get stuck in the while loop until both limit switches are open. Whie 'stuck' in the while loop it reads serial 
       communication which prevents errors and allows an easy way to exit the loop. I had it working Except that after a long period
       of time, RRCI would throw a communication error relative to semiphore timeout period. in attempting to resolve this, I added
       the readserial() function to the while loop and made other changes, the sketch returned to behaving badly.


    -If the appropriate limit switch does not close within MOVE_TIMEOUT(indicating that roof movement failed to complete);
      -RRCI and NINA both Report ERROR
      -IF the appropriate limit switch, then closes, both report the New Roof state (Open or Closed), Fault is cleared
      -If the opposite limit switch, then closes, both report the New original state (Open or Closed),Fault is cleared

  -Simulating Roof Operation with a key fob type remote, or a manual pushbutton switch (with NINA and RRCI still connected)
    -Both limits open (during roof movement) 
      -RRCI interface reports IDLE (odd but not an issue)
      -NINA status does not change, still reports Open or closed depending upon previous roof position (possibly an issue)

    -Only OPEN or CLOSE limit switch closed
      -RRCI reports Opened or Closed respectively
      -NINA reports Open or Closed respectively

  -Both limits closed (simulating a bad switch or a short in the wiring)
    -Always reports OPEN on NINA and RRCI regardless if the roof was previously Open or closed (ISSUE4)

  -When safety switch(s) are open (PIN_SAFE == HIGH)
    -LED not lit as mentioned above in LED behavior (note-no other indication of safe mode or lack thereof exist on the RRCI display - may be an oversight)
    -However, If OPEN or CLOSE shutter is requested by NINA
     -Relay's do not actuate to move the roof (as expeted)
     -Status in both NINA and RRCI are updated to Opening or Closing respectively (no Error reported, again odd, but not an issue)



SFH 7-14-26 to 7-21-26
=========================================================
*/

#include <string.h>

// ======================================================
// PINS
// ======================================================

#define PIN_MOTION          2
#define RELAY_FAN           4 //added for Fan control relay
#define RELAY_CLOSE         6 //not used
#define RELAY_OPERATOR      7 //added for Operator power relay
#define RELAY_OPEN          8 //Relay O-C

#define LED_PIN            9
#define PIN_OPENED         10
#define PIN_CLOSED         11
#define RELAY_STOP         12
#define PIN_SAFE           13

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

enum ControllerType
{
    ALEKO_SINGLE_BUTTON = 1,
    OPEN_CLOSE = 2,
    OPEN_CLOSE_STOP = 3
};

ControllerType controllerType =
    ALEKO_SINGLE_BUTTON;

// ======================================================
// GLOBALS
// ======================================================

unsigned long moveStart = 0;
unsigned long lastMotionTime = 0;
unsigned long lastSerialByteTime = 0;

volatile unsigned long motionPulseCount = 0;

bool safeModeEnabled = true; //I changed this to true to resolve ISSUE1
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

void PulseOpenRelay();
void PulseCloseRelay();
void PulseStopRelay();
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
  pinMode(RELAY_OPEN, OUTPUT);
  pinMode(RELAY_CLOSE, OUTPUT);
  pinMode(RELAY_STOP, OUTPUT);
  pinMode(RELAY_FAN, OUTPUT);
  pinMode(RELAY_OPERATOR, OUTPUT); //added for operator power relay

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

  GableFanControl();
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
  // Controller Mode
  // --------------------------------------------------

  if (strcmp(cmd, "setmode:1") == 0)
  {
  controllerType = ALEKO_SINGLE_BUTTON;
  Ack("setmode");
  return;
  }

  if (strcmp(cmd, "setmode:2") == 0)
  {
  controllerType = OPEN_CLOSE;
  Ack("setmode");
  return;
  }

  if (strcmp(cmd, "setmode:3") == 0)
  {
  controllerType = OPEN_CLOSE_STOP;
  Ack("setmode");
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
  // Commanded motion
  // --------------------------------------------------

  if (state == OPENING || state == CLOSING)
  {
    bool openActive = IsOpen();
    bool closedActive = IsClosed();

    // Both limits active indicates a wiring or sensor fault.
    if (openActive && closedActive)
    {
      state = ERROR;
      digitalWrite(RELAY_OPERATOR, LOW);
      return;
    }

    // Only the destination limit completes travel. The starting limit is
    // expected to remain active briefly while the roof begins moving.
    if (state == OPENING && openActive)
    {
      state = OPEN;
      digitalWrite(RELAY_OPERATOR, LOW);
      return;
    }

    if (state == CLOSING && closedActive)
    {
      state = CLOSED;
      digitalWrite(RELAY_OPERATOR, LOW);
      return;
    }

    // Hard timeout
    if (now - moveStart > MOVE_TIMEOUT)
    {
      state = ERROR;
      digitalWrite(RELAY_OPERATOR, LOW);
      return;
    }

    // Motion timeout
    if (motionSensorEnabled)
    {
      if (now - lastMotionTime >
          MOTION_CHECK_INTERVAL)
      {
        state = ERROR;
        digitalWrite(RELAY_OPERATOR, LOW);
        return;
      }
    }

    // Do not allow the starting limit to overwrite the commanded direction.
    return;
  }

  //--------------------------------------------------
  //Shorted switch or wiring,  //I added to detect shorted switches or wiring (ISSUE4)
  //--------------------------------------------------

  if (IsOpen() && IsClosed()) 
  { 
    state = ERROR;
    digitalWrite(RELAY_OPERATOR, LOW);

    return;
  }

  // --------------------------------------------------
  // Open Sensor
  // --------------------------------------------------

  if (IsOpen())
  {
    state = OPEN;
    digitalWrite(RELAY_OPERATOR, LOW);

    return;
  }

  // --------------------------------------------------
  // Closed Sensor
  // --------------------------------------------------

  if (IsClosed())
  {
    state = CLOSED;
    digitalWrite(RELAY_OPERATOR, LOW);
    
    return;
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

  // Reset pulse count only when opening
  motionPulseCount = 0;

  lastMotionState = false;

  switch(controllerType)
  {
    case ALEKO_SINGLE_BUTTON:

        PulseTriggerRelay();
        break;

    case OPEN_CLOSE:

        PulseOpenRelay();
        break;

    case OPEN_CLOSE_STOP:

        PulseOpenRelay();
        break;
  }
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

  // DO NOT reset pulse count here
  // We need existing pulse position
  // for proper close percentage tracking

  lastMotionState = false;

  switch(controllerType)
  {
    case ALEKO_SINGLE_BUTTON:

      PulseTriggerRelay();
      break;

    case OPEN_CLOSE:

      PulseCloseRelay();
      break;

    case OPEN_CLOSE_STOP:

      PulseCloseRelay();
      break;
  }
}
// ======================================================
// RELAY PULSE
// ======================================================

void PulseTriggerRelay()
{
  digitalWrite(RELAY_OPERATOR, HIGH);//added to turn operator power on for travel

  delay(100);//added for operator power delay

  digitalWrite(RELAY_OPEN, LOW);

  delay(100);

  digitalWrite(RELAY_OPEN, HIGH);

  delay(RELAY_PULSE_TIME);

  digitalWrite(RELAY_OPEN, LOW);
}

// ======================================================
// V2 RELAY FUNCTIONS
// ======================================================

void PulseOpenRelay()
{
  digitalWrite(RELAY_OPERATOR, HIGH);//added to turn operator power on for travel

  delay(100);//added for operator power delay

  digitalWrite(RELAY_OPEN, HIGH);

  delay(RELAY_PULSE_TIME);

  digitalWrite(RELAY_OPEN, LOW);
}

void PulseCloseRelay()
{
  digitalWrite(RELAY_OPERATOR, HIGH);//added to turn operator power on for travel

  delay(100);//added for operator power
  
  digitalWrite(RELAY_CLOSE, HIGH);

  delay(RELAY_PULSE_TIME);

  digitalWrite(RELAY_CLOSE, LOW);
}

void PulseStopRelay()
{
  digitalWrite(RELAY_STOP, HIGH);

  delay(RELAY_PULSE_TIME);

  digitalWrite(RELAY_STOP, LOW);
}

// ======================================================
// STOP
// ======================================================

void StopAll()
{
  switch(controllerType)
  {
    case ALEKO_SINGLE_BUTTON:

      PulseTriggerRelay();
      break;

    case OPEN_CLOSE:

      // No dedicated stop input
      break;

    case OPEN_CLOSE_STOP:

      PulseStopRelay();
      break;
  }

  StopAllRelays();

  lastMotionState = false;

  // Reset timeout tracking
  lastMotionTime = millis();

  if (IsOpen())
    state = OPEN;
  else if (IsClosed())
    state = CLOSED;
  else
    state = IDLE;
}

void StopAllRelays()
{
  digitalWrite(RELAY_OPEN, LOW);
  digitalWrite(RELAY_CLOSE, LOW);
  digitalWrite(RELAY_STOP, LOW);
  digitalWrite(RELAY_FAN, LOW); //added for fan relay
  digitalWrite(RELAY_OPERATOR, LOW);//added for operator power relay
}

// ======================================================
// STATUS
// ======================================================

void SendStatus()
{
  Serial.print("STATE:");

  // Preserve the commanded direction while leaving the starting limit.
  if (state == OPENING)
  {
    Serial.print("OPENING;");
  }
  else if (state == CLOSING)
  {
    Serial.print("CLOSING;");
  }
  else if (state == ERROR || (IsOpen() && IsClosed()))
  {
    Serial.print("ERROR;");
  }
  else if (IsOpen())
  {
    Serial.print("OPEN;");
  }
  else if (IsClosed())
  {
    Serial.print("CLOSED;");
  }
  else
  {
    Serial.print("IDLE;");
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

//--------------------------
// Gable fan control
//--------------------------
void GableFanControl()
{
 if (IsClosed() == false) { //if LS_closed switch reads as OPEN contact indicating that the roof is NOT closed
    digitalWrite(RELAY_FAN, HIGH); // Turn the relay K1 on, in order to turn the gable fan off (N.C. contact) as soon as the roof starts to open (no need or desire to have the gable fan running if roof is open)
  } //end of if roof is NOT LS_closed

  else { //if LS_closed switch reads as CLOSED contact indicating that the roof is closed
   digitalWrite(RELAY_FAN, LOW); // Turn the relay K1 off, in order to turn the gable fan on as soon as the roof is closed
  }
}// end of fan control function
