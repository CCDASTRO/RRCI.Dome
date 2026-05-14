This project is an ASCOM Dome driver for a roll-off-roof telescope observatory that is controlled using an Arduino Uno, it is released under the Cretative Commons Zero 1.0 Universal license, except that commercial use is prohibited without prior permission.
1. Arduino Firmware Changes

the following features were added.

New Hardware Input
Hall-Effect Motion Sensor
Connected to Arduino pin D2
Detects roof movement using a magnet attached to a wheel or pulley
Generates pulses while the roof is moving
New Runtime Options
Scope Safe

Controlled by:

setsafe:1
setsafe:0

When disabled, the safe input is ignored and always treated as safe.

Motion Sensor

Controlled by:

setmotion:1
setmotion:0

When disabled, motion pulses are not required.

Motion Stall Detection

When Motion Sensor is enabled:

Firmware expects motion pulses while opening or closing
If no pulses are detected for 3 seconds, state changes to ERROR
Motion Pulse Count

Firmware counts pulses and includes them in the status response:

STATE:OPENING;SAFE;PULSES:12;MOVING#
New Serial Commands
Command	Function
setsafe:1	Enable scope safety
setsafe:0	Disable scope safety
setmotion:1	Enable Hall sensor monitoring
setmotion:0	Disable Hall sensor monitoring

2. Dome.cs Changes

The ASCOM dome driver was enhanced significantly.

ASCOM Trace Logging

Added:

private readonly TraceLogger tl;

The driver logs:

Connection events
Serial commands sent and responses received
State changes
Errors and timeouts

Logging is controlled by the TraceLogger profile setting.

New Profile Settings Read
Setting	Purpose
COM	Serial port
Baud	Baud rate
SafeMode	Enable scope safe input
MotionSensor	Enable Hall sensor
TraceLogger	Enable ASCOM logging
Runtime Configuration Commands

After connecting, the driver sends:

setsafe:1 or setsafe:0
setmotion:1 or setmotion:0
Improved Serial Query Method

Centralized Query() method:

Sends command
Waits for response
Logs TX/RX
Enforces timeout
Improved Shutter State Machine

The driver now:

Tracks whether an open or close command is active
Waits for confirmation from limit switches
Uses a 2-second grace period after issuing commands
Times out after 120 seconds
Returns shutterError if something goes wrong
Motion Sensor Integration

The driver does not need to count pulses itself. The firmware monitors movement and reports ERROR if motion stops.

3. SetupDialogForm.cs Changes
New Checkbox

Added:

chkMotionSensor
Existing Checkbox Used

Already present:

chkTraceLogging
Settings Loaded and Saved

The setup dialog now loads and saves:

COM
Baud
DeviceId
SafeMode
RainSensor
AutoClose
MotionSensor
TraceLogger

4. User Workflow
Open the ASCOM Setup Dialog.
Select COM port and baud rate.
Enable or disable:
Scope Safe
Motion Sensor
Trace Logging
Click OK.
Settings are stored in the ASCOM Profile.
On connection:
Driver reads settings.
Driver enables or disables features in the Arduino.
During operation:
Firmware monitors limit switches.
Optional Hall sensor confirms motion.
Errors are reported if movement stalls.

5. Backward Compatibility

If both checkboxes are unchecked:

Firmware behaves exactly like your original version.
No Hall sensor is required.
Safe input is ignored.

6. Recommended Hardware for Hall Sensor
A magnet attached to a moving wheel, pulley, or sprocket
Hall-effect sensor mounted nearby
Output wired to Arduino D2
GND and +5V as required by the sensor module

7. Error Detection Improvements

The system can now detect:

Roof stalled mechanically
Broken belt/chain
Motor running but roof not moving
Unsafe scope position
Motion timeout
Missing serial response

8. ASCOM Trace Logs

When Trace Logging is enabled, ASCOM logs include:

Connection details
Commands sent
Responses received
State transitions
Exceptions

These logs are useful for troubleshooting.

Final Result

RRCI now provides:

ASCOM-compliant dome control
Optional scope safety interlock
Optional Hall-effect motion verification
Automatic stall detection
Detailed trace logging
Robust serial communications
Full backward compatibility with existing hardware

This with the hall effect movement sensor makes the system substantially more reliable and better able to detect real mechanical problems during roof operation.
