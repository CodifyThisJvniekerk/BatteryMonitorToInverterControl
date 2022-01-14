This code is primarily for my own personal use.
The use case is there exist a tool to monitor the LBSA Battery on a windows machine.
But the inverter axpert (Kodak OG 5.48) does not support CAN BUS 2 that the battery comunicates in.
So this is to play the middle man with a windows PC since the back to grid voltage can't be set high enough to not discharge the battery under 20% state of charge.
It will force back to grid by means of changing the Output source priority to SUB for stop discharge and SBU for discharge battery. 
