#!/bin/bash

source $FCT_RUN_FOLDER/setup.sh

trap ctrl_c INT

function ctrl_c() {
        echo
        echo "Aborting"
        echo "Turning off HV ..."
        source set_HV_setup_$WHICHSETUP.sh OFF
        echo "Turning off pulse gen ..." 
        echo "OUTPUT OFF" | cat > /dev/ttyACM0
        echo "OUTPUT OFF" | cat > /dev/ttyACM0
        echo "DO NOT disconnect FEB if LV is on"
        exit;
}


if [ -z $(pidof mono) ]
then 
  echo
else
  echo "Terminating previously open GUI..."
  sudo kill $(pidof mono)
  echo
fi



# Check that the pulse generator is connected. Otherwise, abort script
if bash $FCT_UTILS/check_fg.sh | grep -q '/dev/ttyACM0'; 
then
  echo "Pulse Gen is connected to: /dev/ttyACM0" 
else
  read -p "WARNING: Pulse Gen is NOT connected. Continue for analysis only? (y=yes, any other key=no) " -n 1 -r 
  echo 
  if [[ $REPLY =~ ^[Yy]$ ]]
  then
    # Ask the user for the FEB Serial Number
    echo "Enter serial number (for analysis only):"
    read sn
    #    echo "Running analysis on existing files"
    #    bash run_fct_analysis.sh $sn $bl1 $bl2
    #    exit
  else
    exit
  fi
fi


# Ask the user for the FEB Serial Number
echo "Enter MIB serial number:"
read sn
#This file indentifies the end of script (to close the GUI)
dummy_EOS="MIB_"$sn"_EndOfScript.txt"
MIBDATADIR=$MIBDATADIR/SN_$sn/
[ ! -d "$MIBDATADIR" ] && mkdir "$MIBDATADIR"
#give rwe permission to data folder
sudo chmod 777 $MIBDATADIR

if [[ (-f $MIBDATADIR$dummy_EOS) ]]
then 
  echo
  read -p "Files already present for this SN. Do you want to overwrite?  (y=yes, any other key=no) " -n 1 -r
  echo    # (optional) move to a new line
  if [[ $REPLY =~ ^[Yy]$ ]]
  then
    #turn on HV
    source set_HV_setup_$WHICHSETUP.sh "15"
    #Remove the "EndOFScript.txt" dummy file if it exists already in the directory
    rm -f $MIBDATADIR$dummy_EOS
    bash $FCT_UTILS/MIBLaunchScript.sh $sn

    #turn off HV
    source set_HV_setup_$WHICHSETUP.sh OFF 

    bash $FCT_UTILS/MIBLaunchAnalysis.sh $sn
  else
    echo
    echo "Running analysis on existing files"
    echo
    bash $FCT_UTILS/MIBLaunchAnalysis.sh $sn
    exit
  fi
else
    #turn on HV
    source set_HV_setup_$WHICHSETUP.sh "15"
  rm -f $MIBDATADIR$dummy_EOS;
  bash $FCT_UTILS/MIBLaunchScript.sh $sn

  #turn off HV
  source set_HV_setup_$WHICHSETUP.sh OFF 

  bash $FCT_UTILS/MIBLaunchAnalysis.sh $sn


  exit
fi





