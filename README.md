observatory automation system with layered safety, telemetry, and live monitoring.

Firmware Enhancements

The Arduino firmware was expanded to support:

Hall-effect pulse counting
Edge-triggered motion detection
getpulsecount serial command
Motion watchdog support
Overshoot-capable telemetry
Improved serial reliability
Reconnect-safe command handling
Cleaner movement state tracking

The firmware now supports both:

basic limit-switch-only operation,
and enhanced pulse-monitored operation.
ASCOM Driver Enhancements

The Dome.cs driver was significantly upgraded.

Added Shared Telemetry System

A new shared telemetry layer was created:

RoofTelemetry

to centralize:

roof state,
pulse count,
percent open,
faults,
movement timing,
limit states.

This allows:

driver logic,
UI,
watchdogs,
and diagnostics

to all share the same state cleanly.

Shutter State Machine Improvements

ShutterStatus was upgraded to:

synchronize telemetry,
handle fault propagation,
update motion state,
expose live roof status,
and preserve backward compatibility.
Pulse Polling System

The driver now:

polls Arduino pulse count,
calculates roof percentage,
tracks last pulse timing,
verifies active movement.

This added:

real roof position estimation,
pulse-based movement verification.
Safety Improvements
Hall Pulse Stall Detection

If the roof is commanded to move but pulses stop arriving:

movement aborts,
fault state is raised.

This protects against:

jams,
motor stalls,
slipping mechanisms,
broken hall wiring.
Hard Failsafe Timeout

A separate absolute movement timeout was retained.

This protects against:

firmware hangs,
serial failures,
logic failures,
unexpected edge cases.

You now have layered protection:

pulse-based verification,
plus absolute timeout backup.
Overshoot Protection

The driver now detects:

pulse counts exceeding calibrated open travel.

This protects against:

failed limit switches,
runaway relays,
uncontrolled motion.
SetupDialog Improvements

The configuration UI was expanded to support:

hall sensor enable/disable,
pulse calibration storage,
persistent telemetry configuration.

The driver now stores:

fully-open pulse count,
motion sensor enable state,
existing ASCOM settings.
Live Status Window

A new diagnostics/status window was added.

Features:

auto-open on connect,
auto-close on disconnect,
always-on-top utility display,
live telemetry updates.

The UI displays:

roof state,
pulse count,
percent open,
fault status,
progress bar.

The window is driver-controlled and cannot accidentally be closed.

Backward Compatibility

The system still fully supports:

operation without a hall sensor,
traditional limit-switch-only control.

If the motion sensor option is disabled:

pulse logic is bypassed,
original behavior remains intact.
Final Result

The final system now includes:

ASCOM IDomeV2 compatibility
NINA compatibility
telemetry-driven monitoring
pulse-based movement verification
layered fault protection
live diagnostics UI
reconnect-safe serial handling
observatory-grade safety logic

The project evolved from:

a simple relay roof controller

into:

a real observatory automation roof control system suitable for unattended operation.
Firmware Enhancements

The Arduino firmware was expanded to support:

Hall-effect pulse counting
Edge-triggered motion detection
getpulsecount serial command
Motion watchdog support
Overshoot-capable telemetry
Improved serial reliability
Reconnect-safe command handling
Cleaner movement state tracking

The firmware now supports both:

basic limit-switch-only operation,
and enhanced pulse-monitored operation.
ASCOM Driver Enhancements

The Dome.cs driver was significantly upgraded.

Added Shared Telemetry System

A new shared telemetry layer was created:

RoofTelemetry

to centralize:

roof state,
pulse count,
percent open,
faults,
movement timing,
limit states.

This allows:

driver logic,
UI,
watchdogs,
and diagnostics

to all share the same state cleanly.

Shutter State Machine Improvements

ShutterStatus was upgraded to:

synchronize telemetry,
handle fault propagation,
update motion state,
expose live roof status,
and preserve backward compatibility.
Pulse Polling System

The driver now:

polls Arduino pulse count,
calculates roof percentage,
tracks last pulse timing,
verifies active movement.

This added:

real roof position estimation,
pulse-based movement verification.
Safety Improvements
Hall Pulse Stall Detection

If the roof is commanded to move but pulses stop arriving:

movement aborts,
fault state is raised.

This protects against:

jams,
motor stalls,
slipping mechanisms,
broken hall wiring.
Hard Failsafe Timeout

A separate absolute movement timeout was retained.

This protects against:

firmware hangs,
serial failures,
logic failures,
unexpected edge cases.

You now have layered protection:

pulse-based verification,
plus absolute timeout backup.
Overshoot Protection

The driver now detects:

pulse counts exceeding calibrated open travel.

This protects against:

failed limit switches,
runaway relays,
uncontrolled motion.
SetupDialog Improvements

The configuration UI was expanded to support:

hall sensor enable/disable,
pulse calibration storage,
persistent telemetry configuration.

The driver now stores:

fully-open pulse count,
motion sensor enable state,
existing ASCOM settings.
Live Status Window

A new diagnostics/status window was added.

Features:

auto-open on connect,
auto-close on disconnect,
always-on-top utility display,
live telemetry updates.

The UI displays:

roof state,
pulse count,
percent open,
fault status,
progress bar.

The window is driver-controlled and cannot accidentally be closed.

Backward Compatibility

The system still fully supports:

operation without a hall sensor,
traditional limit-switch-only control.

If the motion sensor option is disabled:

pulse logic is bypassed,
original behavior remains intact.
Final Result

system now includes:

ASCOM IDomeV2 compatibility
NINA compatibility
telemetry-driven monitoring
pulse-based movement verification
layered fault protection
live diagnostics UI
reconnect-safe serial handling
observatory-grade safety logic

