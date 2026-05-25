The project evolved from a basic Arduino relay-controlled observatory roof into a full ASCOM-compatible automated roof control system with telemetry, layered safety protections, and live diagnostics.
The Arduino firmware was enhanced to support:
•	limit switches, 
•	optional hall-effect motion sensing, 
•	pulse counting, 
•	watchdog timeouts, 
•	and reliable serial communication. 
The ASCOM dome driver was upgraded with:
•	real-time roof telemetry, 
•	pulse-based movement verification, 
•	roof percentage calculation, 
•	hall timeout detection, 
•	hard failsafe timeouts, 
•	overshoot/runaway protection, 
•	reconnect-safe serial handling, 
•	and full NINA compatibility. 
A shared telemetry system was added so:
•	the driver, 
•	watchdog logic, 
•	and UI 
all share synchronized roof state information.
The setup UI was expanded to allow:
•	motion sensor enable/disable, 
•	pulse calibration, 
•	and persistent driver configuration. 
A live status window was added that:
•	automatically opens on connect, 
•	closes on disconnect, 
•	stays on top, 
•	and displays: 
o	roof state, 
o	percent open, 
o	pulse count, 
o	progress bar, 
o	and fault status. 
The final system supports both:
•	simple limit-switch-only operation, 
•	advanced pulse-monitored unattended observatory automation with layered fault protection. 

