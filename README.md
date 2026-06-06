# RRCI Observatory Roof Controller

## Overview

The Rolling Roof Controller Interface (RRCI) is an Arduino-based observatory roof automation system designed for reliable unattended operation of roll-off roof observatories.

RRCI combines custom firmware with a fully ASCOM-compliant Dome driver, allowing seamless integration with astronomy automation platforms such as NINA, ASCOM Device Hub, and other ASCOM-compatible software.

Originally developed as a simple relay controller, RRCI has evolved into a telemetry-driven roof automation platform featuring:

* Real-time roof position tracking
* Pulse-based calibration and position estimation
* Multi-layer fault protection
* Live diagnostics and monitoring
* Reconnect-safe operation
* Multi-controller support
* Observatory-grade unattended automation

---

# Key Features

## ASCOM Dome Driver

Full ASCOM IDomeV2 compatibility including:

* OpenShutter()
* CloseShutter()
* AbortSlew()
* ShutterStatus
* Connected state management

Compatible with:

* NINA
* ASCOM Device Hub
* ASCOM Remote
* Other ASCOM automation platforms

---

## Arduino Roof Controller

Features include:

* USB serial communications
* Relay-based motor control
* Heartbeat monitoring
* Command-response protocol
* Reconnect-safe architecture
* Controller type selection

Supported controller architectures:

### Mode 1 – Aleko Single Button

Single relay operation compatible with Aleko gate controllers.

### Mode 2 – Open / Close

Dedicated Open and Close control outputs.

### Mode 3 – Open / Close / Stop

Dedicated Open, Close, and Stop control outputs.

Controller mode may be selected through the ASCOM Setup Dialog or serial command interface.

---

# Roof Position Tracking

## Hall Pulse Telemetry

Optional hall-effect motion sensing provides:

* Motion verification
* Pulse counting
* Roof position estimation
* Stall detection
* Overshoot protection

During calibration the roof is moved from fully closed to fully open and the total pulse count is recorded.

The calibrated pulse count is stored and automatically restored on future connections.

---

## Limit Switch Verification

Physical Open and Closed limit sensors provide positive confirmation of roof position.

The system uses both:

* Hall pulse telemetry
* Open/Closed limit sensors

to provide reliable roof position tracking.

When limit sensors indicate a fully open or fully closed roof, they are treated as the authoritative position source.

---

## Reconnect Recovery

If the controller reconnects after a restart and pulse information is unavailable:

* Open sensor active → Roof restored to 100% open
* Closed sensor active → Roof restored to 0% open

Previously calibrated pulse counts are restored automatically to maintain accurate position reporting.

---

# Roof Safety Systems

## Hard Movement Timeout

Stops roof motion if travel exceeds the configured maximum movement time.

Protects against:

* Stuck relays
* Failed motor controllers
* Communication errors
* Runaway roof movement

---

## Hall Pulse Stall Detection

When pulse telemetry is enabled, the driver continuously verifies roof movement.

If motion is commanded but pulses stop unexpectedly, the driver:

* Stops motion
* Enters fault state
* Reports an error to ASCOM clients

Protects against:

* Mechanical jams
* Failed motors
* Slipping drive systems
* Failed hall sensors

---

## Overshoot Protection

Continuously monitors pulse travel distance.

If travel exceeds the calibrated full-open pulse count plus tolerance, the driver:

* Stops motion
* Reports a fault condition

Protects against:

* Failed limit switches
* Runaway relays
* Controller malfunctions

---

# Telemetry System

The telemetry layer continuously tracks:

* Roof state
* Open limit status
* Closed limit status
* Hall pulse count
* Percent open
* Motion state
* Movement timing
* Fault conditions

Telemetry is synchronized across:

* Arduino firmware
* ASCOM driver
* Diagnostics user interface

---

# Live Diagnostics Window

A dedicated diagnostics window provides real-time operational monitoring.

## Displays

* Roof state
* Percent open
* Hall pulse count
* Motion status
* Fault information
* Progress bar
* Calibration status

## Behavior

* Automatically opens on driver connection
* Automatically closes on disconnect
* Runs independently of ASCOM operations
* Remains responsive during calibration and roof movement
* Designed as a lightweight utility window

---

# Calibration System

RRCI includes built-in pulse calibration.

## Calibration Process

1. Ensure the roof is fully closed.
2. Start calibration from the Status Window.
3. The roof opens completely.
4. Total travel pulses are measured.
5. The calibrated pulse count is automatically saved.

Calibration values persist across:

* Driver restarts
* NINA restarts
* Computer reboots

No recalibration is required unless roof mechanics are changed.

---

# Setup & Configuration

The ASCOM Setup Dialog provides configuration for:

* COM Port
* Baud Rate
* Controller Type
* Safe Mode
* Motion Sensor Enable
* Pulse Calibration
* Trace Logging

All settings are automatically saved and restored.

---

# Recommended Hardware

## Supported Components

* Arduino Uno
* Arduino Nano
* USB serial interface
* Relay module
* Hall-effect sensor (optional)
* Open limit switch
* Closed limit switch
* Roll-off roof motor controller

---

# System Architecture

```text
NINA / ASCOM Client
          ↓
ASCOM Dome Driver
          ↓
Telemetry & Safety Layer
          ↓
USB Serial Communications
          ↓
Arduino Roof Controller
          ↓
Motor Controller Relays
          ↓
Roof Sensors
          ↓
Observatory Roof
```

---

# Fault Handling Philosophy

RRCI is designed around:

* Fail-safe operation
* Layered protection
* Defensive programming
* Graceful recovery

Multiple independent safety systems ensure:

* No single sensor failure can cause uncontrolled roof movement
* Faults are immediately surfaced to automation software
* Recovery after disconnects and controller restarts is automatic
* Unattended observatory operation remains reliable

---

# Project Highlights

* Custom ASCOM Dome Driver
* Arduino-based roof controller
* Multi-controller support
* Pulse-based calibration
* Live telemetry system
* Real-time diagnostics window
* Hall pulse motion verification
* Stall detection
* Overshoot protection
* Reconnect-safe architecture
* Persistent configuration storage
* NINA-compatible unattended automation

---

# License

Personal and educational observatory automation project.

---

# Author

Chuck Faranda

CCD Astro Observatory Automation

https://ccdastro.net
