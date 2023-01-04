#### PRELIMINARY PROCEDURE: compile root analysis
    $ cd /home/neutrino/FCT/FunctionalTest/build
    $ bash cmake_clean.sh
    $ cmake ..
    $ make

#### Procedure to run Functional Test
# Open a terminal in root mode: 
    $ sudo -i 
    Insert password: UniGeneve2022
# Go to script folder: 
    $ cd /home/neutrino/FCT/code/scripts/FCT/Linux/
# Run bash script
    $ bash Socket.sh 
    or
    $ time bash Socket.sh
    the GUI should be opening. (use "time") to display the total excution time at the end of script

# BUG HINT:
    Error : 'Object reference not set to an instance of an object' -> GPIO might be OFF

    Check that the socket window says: "waiting for a new single client connection"

# BUG HINT:
    If the socket server stops at start up (you get "server: stopped" when you open the GUI), try to change the number of the port from the app-settings.json file contained in the GUI directory (same directory as UnigeGpioBoard.exe) Numbers accepted ONLY above 10000

# BUG HINT:
    [SCR_MSG]:[[ERR]:[A script is running. First abort the script by sending 'ESC+A, Enter' before sending another request !]  -> this error does not prevent the script from running correctly

Insert the serial number of the FEB under test.

If the folder corresponding to the inserted serial number is already filled with data, the user is asked to overwrite the data (y) or not (any other key). If the user choses to NOT overwrite, the GUI is closed and the analysis is ran on the existing data.

Wait for the script to finish