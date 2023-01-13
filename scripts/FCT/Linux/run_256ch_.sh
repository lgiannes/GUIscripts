#!/bin/bash

########### To measure execution time #####
start=`date +%s`
###########################################


read -p "Run Loopback/Housekeeping test? " -n 1 -r 
echo 
if [[ $REPLY =~ ^[Yy]$ ]]
then
  echo "Enter serial number:"
  read sn
  bash Run_LBHK_test.sh $sn
  read -p "Go on with other tests? " -n 1 -r 
  echo
  if [[ $REPLY =~ ^[Yy]$ ]]
  then
    echo "Starting other tests ..."
  else
    exit
  fi  
fi


export DATADIR="/home/neutrino/FCT/data_local/"
# For BASELINE test:
bl1=32000
bl2=50000

#This file indentifies the end of script (to close the GUI)
dummy_EOS="EndOfScript.txt"
dummy_EOS_citi="EndOfScript_citi.txt"

#Kill all the jobs (avoid double serial communication)
  # sudo kill $(pidof mono)

# Check that the pulse generator is connected. Otherwise, abort script
if bash check_fg.sh | grep -q '/dev/ttyACM0'; 
then
  echo "Pulse Gen is connected to: /dev/ttyACM0" 
else
  read -p "ERROR: Pulse Gen is NOT connected. Continue? (y=yes, any other key=no) " -n 1 -r 
  echo 
  if [[ $REPLY =~ ^[Yy]$ ]]
  then
    # Ask the user for the FEB Serial Number
    echo "Enter serial number:"
    read sn
    export DATADIR=$DATADIR"SN_"$sn"/"
    echo "DATADIR: "$DATADIR
    sudo chmod 777 $DATADIR
    # if [[ (-f $DATADIR$dummy_EOS) && (-f $DATADIR$dummy_EOS_citi) ]]    
    # then 
       echo "Running analysis on existing files"
       bash run_256ch_analysis.sh $sn $bl1 $bl2
       exit
    # else
    #   echo "no data"
    #   exit
    # fi
  else
    exit
  fi
fi


# Ask the user for the FEB Serial Number
echo "Enter serial number:"
read sn

# Print out data folder and give rwe permission
export DATADIR=$DATADIR"SN_"$sn"/"
echo "DATADIR: "$DATADIR
sudo chmod 777 $DATADIR

if [[ (-f $DATADIR$dummy_EOS) && (-f $DATADIR$dummy_EOS_citi) ]]
then 
  read -p "Files already present for this SN. Do you want to overwrite?  (y=yes, any other key=no) " -n 1 -r
  echo    # (optional) move to a new line
  if [[ $REPLY =~ ^[Yy]$ ]]
  then
    #Remove the "EndOFScript.txt" dummy file if it exists already in the directory
    rm -f $DATADIR$dummy_EOS
    rm -f $DATADIR$dummy_EOS_citi
    bash run_256ch_data_taking.sh $sn $bl1 $bl2
    bash run_256ch_analysis.sh $sn $bl1 $bl2
  else
    echo "Running analysis on existing files"
    bash run_256ch_analysis.sh $sn $bl1 $bl2
    exit
  fi
else
  rm -f $DATADIR$dummy_EOS;
  rm -f $DATADIR$dummy_EOS_citi;
  bash run_256ch_data_taking.sh $sn $bl1 $bl2;
  bash run_256ch_analysis.sh $sn $bl1 $bl2;
  echo "EXIT"
  exit;
fi

########### To measure execution time #####
end=`date +%s`
###########################################
# mins=(`expr $end - $start`)/60
# secs=(`expr $end - $start`)%60

# echo 
# echo 
# echo Execution time was  and  seconds.




