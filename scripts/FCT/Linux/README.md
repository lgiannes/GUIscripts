### RUN THE SCRIPTS. The list of all the scripts and description follows
# WARNING: Never run scripts with source or ./ 
# Always run them in a subshell (bash ...)

## Complete Functional test
# the main script of this package is Functional_Test.sh
# it can be run without arguments to have a full functional test + calibration of the board:
time bash Functional_Test.sh
# duration: around 5'30"
# For the first test of the boards, before installation of the coldplate, the FCT has to be # ran WITHOUT calibration. You can do so by adding the argument "NOCALIB":
time bash Functional_Test.sh NOCALIB
# duration: around 6'30"

## Housekeeping/Loopback only
# this script runs the first part of the test. Some hardware actions are required!
# provide serial number as argument!
time bash FCT_LBHK_test.sh SN

## 256ch test
# this script tests all the 256 analog channels and the well functioning of baseline change
time bash FCT_256ch_test.sh

## FCT_CITIROC_test.sh
# this script tests the different trigger options of the CITIROCs
# provide serial number as argument!
time bash FCT_CITIROC_test.sh SN

## Calibration
# this sript runs the calibration routine for the FEB
# provide serial number as argument!
time bash FCT_Calibration.sh SN

## Analysis only
# this script runs teh analysis for a certain serial number for which the data taking part of the test (or the whole test) has already been done
# provide serial number as argument!
time bash Analysis_only.sh SN

## MIB test
# this script test the MIB. Warning: this is supposed to be run with the MIB type adapter board (look at the label at the bottom lef tof the big adapter board (connected to the GPIO))
time bash MIBtest.sh 

## Show Results
# this script shows the results for a certain serial number for which the FCT (complete) has alrady been done
# provide serial number as argument!
bash ShowResults.sh SN
# this is automatically launched at the end of each of the scripts above

# it is always good practice to source setup.sh in the terminal where the scripts are being ran
# all these scripts use subscripts and c sharp code contained in the ./utils/ folder


### Test Bench set up

MANDATORY to connect the GPIO to a USB3 port. USB2 is not supported

## ADD INSTRUCTIONS FOR THE SCRIPTS!

#### PRELIMINARY PROCEDURE: compile root analysis
    cd /home/neutrino/FCT/FunctionalTest/build
    bash cmake_clean.sh
    cmake ..
    make

#### Procedure to run Functional Test
# Open a terminal in root mode: 
    sudo -i 
Insert password: UniGeneve2022
# Go to script folder: 
    cd /home/neutrino/FCT/code/scripts/FCT/Linux/
# Run bash script

    time bash Functional_Test.sh
the GUI should be opening. (use "time") to display the total excution time at the end of script

# BUG HINT:
Error : 'Object reference not set to an instance of an object' -> GPIO might be OFF

Check that the socket window says: "waiting for a new single client connection"

# BUG HINT:
If the socket server stops at start up (you get "server: stopped" when you open the GUI), try to change the number of the port from the app-settings.json file contained in the GUI directory (same directory as UnigeGpioBoard.exe) Numbers accepted ONLY above 10000

Insert the serial number of the FEB under test.

If the folder corresponding to the inserted serial number is already filled with data, the user is asked to overwrite the data (y) or not (any other key). If the user choses to NOT overwrite, the GUI is closed and the analysis is ran on the existing data.

Wait for the script to finish

# Check output
A quick way of checking if the FEB has pased the test is:
    tail -3 data_output_folder/output.os.txt;
    tail -3 data_output_folder/output.ba.txt;
    tail -3 data_output_folder/output.bl.txt;
