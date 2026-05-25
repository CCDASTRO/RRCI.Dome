RRCI Observatory Roof Controller

A robust ASCOM-compatible roll-off roof automation system for unattended observatory operation.

Overview

The Rolling Roof Controller Interface (RRCI) is an Arduino-based observatory roof control system paired with a custom ASCOM IDomeV2 driver for seamless integration with astronomy automation software such as NINA.

The project evolved from a simple relay controller into a telemetry-driven automation platform featuring:

layered fault protection,
pulse-monitored movement verification,
live diagnostics,
watchdog recovery,
and observatory-grade operational safety.
Key Features
ASCOM Dome Driver
Full IDomeV2 compatibility
Native support for:
OpenShutter()
CloseShutter()
AbortSlew()
ShutterStatus
Seamless integration with:
NINA
ASCOM Device Hub
other ASCOM automation platforms
Arduino Roof Controller
Relay-based roof control
Serial command protocol
USB serial communication
Reconnect-safe architecture
Heartbeat monitoring
Roof Safety Systems
Layered Timeout Protection

The controller implements multiple independent safety systems:

Hard Movement Timeout

Stops roof motion if travel exceeds the maximum expected duration.

Hall Pulse Stall Detection

Detects:

motor stalls,
mechanical jams,
slipping drive systems,
failed motion sensors.

If roof movement is commanded but hall pulses stop:

the driver aborts motion,
enters fault state,
and reports an error to ASCOM clients.
Overshoot Protection

Detects excessive pulse travel beyond calibrated roof limits.

Protects against:

failed limit switches,
runaway relays,
uncontrolled motion.
Motion Telemetry System

The system includes a real-time telemetry layer that tracks:

Roof movement state
Open/closed sensor state
Hall pulse count
Roof percentage open
Movement timing
Fault conditions

Telemetry is synchronized across:

Arduino firmware
ASCOM driver
live diagnostics UI
Live Diagnostics Window

A dedicated always-on-top monitor window provides real-time roof diagnostics.

Displays
Roof state
Percent open
Hall pulse count
Fault status
Live progress bar
Behavior
Automatically opens on ASCOM connection
Automatically closes on disconnect
Runs independently from the driver COM thread
Lightweight utility-window design
Optional Hardware Support

The system supports both:

Basic Mode
Limit-switch-only operation
Enhanced Telemetry Mode
Hall-effect motion sensing
Pulse-based movement verification
Position estimation

Motion telemetry can be enabled or disabled in the setup dialog.

Setup & Configuration

The ASCOM setup dialog allows configuration of:

COM Port
Baud Rate
Safe Mode
Motion Sensor Enable
Pulse Calibration
Trace Logging
Recommended Hardware
Supported Components
Arduino Uno / Nano
USB serial interface
Relay module
Hall-effect sensor
Open/closed limit switches
Roll-off roof motor controller
System Architecture
NINA / ASCOM Client
        ↓
ASCOM Dome Driver
        ↓
Telemetry & Safety Layer
        ↓
USB Serial Protocol
        ↓
Arduino Roof Controller
        ↓
Relay Outputs / Sensors
        ↓
Observatory Roof
Fault Handling Philosophy

The controller was designed around:

fail-safe operation,
layered protection,
and graceful recovery.

Multiple independent safety systems ensure:

no single sensor failure can cause uncontrolled roof movement,
faults are surfaced immediately,
and observatory automation remains stable during unattended operation.
Project Highlights
Custom ASCOM Dome Driver
Telemetry-driven roof monitoring
Real-time diagnostics UI
Pulse-based motion verification
Overshoot/runaway protection
Reconnect-safe serial handling
Modular firmware architecture
NINA-compatible unattended automation
License - Personal / educational observatory automation project.

Author - Chuck Faranda https://ccdastro.net
