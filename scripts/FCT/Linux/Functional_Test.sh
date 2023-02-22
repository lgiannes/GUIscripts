#!/bin/bash

source setup.sh

########### To measure execution time #####
start=`date +%s`
###########################################

if [ -z $(pidof mono) ]
then 
  echo
else
  echo "Terminating previously open GUI..."
  sudo kill $(pidof mono)
  echo
fi

GotSN=false
  echo    "/--------------------------------------\\"
  echo    "|                                      |"
  echo    "|   Run Loopback/Housekeeping test?    |" 
  echo    "|      (y=yes, any other key=no)       |" 
  echo    "|                                      |"
  read -p "\\--------------------------------------/" -n 1 -r
  echo
if [[ $REPLY =~ ^[Yy]$ ]]
then
  echo "Enter serial number:"
  read sn
  GotSN=true
  bash FCT_LBHK_test.sh $sn
  echo
  echo    "/--------------------------------------\\"
  echo    "|                                      |"
  echo    "|       Go on with other tests?        |" 
  echo    "|      (y=yes, any other key=no)       |" 
  echo    "|                                      |"
  read -p "\\--------------------------------------/" -n 1 -r 
  echo
  if [[ $REPLY =~ ^[Yy\r]$ ]]
  then
    echo "Starting other tests ..."
  else
    exit
  fi  
fi



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
  echo 
  read -p "WARNING: Pulse Gen is NOT connected. Continue for analysis only? (y=yes, any other key=no) " -n 1 -r 
  echo 
  if [[ $REPLY =~ ^[Yy]$ ]]
  then
    # Ask the user for the FEB Serial Number
    echo "Enter serial number (for analysis only):"
    read sn
    export DATADIR=$GENERALDATADIR"/FEBs/SN_"$sn"/"
    #echo "DATA are stored in:  "$DATADIR
    sudo chmod 777 $DATADIR
    # if [[ (-f $DATADIR$dummy_EOS) && (-f $DATADIR$dummy_EOS_citi) ]]    
    # then 
       echo "Running analysis on existing files"
       bash run_fct_analysis.sh $sn $bl1 $bl2
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
if [ $GotSN = false ]
then 
  echo "Enter serial number:"
  read sn
fi

# Print out data folder and give rwe permission
export DATADIR=$GENERALDATADIR"/FEBs/SN_"$sn"/"
echo "DATA will be stored in: "$DATADIR
sudo chmod 777 $DATADIR

if [[ (-f $DATADIR$dummy_EOS) && (-f $DATADIR$dummy_EOS_citi) ]]
then 
  echo
  read -p "Files already present for this SN. Do you want to overwrite?  (y=yes, any other key=no) " -n 1 -r
  echo    # (optional) move to a new line
  if [[ $REPLY =~ ^[Yy]$ ]]
  then
    #Remove the "EndOFScript.txt" dummy file if it exists already in the directory
    rm -f $DATADIR$dummy_EOS
    rm -f $DATADIR$dummy_EOS_citi
    bash run_fct_data_taking.sh $sn $bl1 $bl2
    bash run_fct_analysis.sh $sn $bl1 $bl2
  else
    echo
    echo "Running analysis on existing files"
    echo
    bash run_fct_analysis.sh $sn $bl1 $bl2
    exit
  fi
else
  rm -f $DATADIR$dummy_EOS;
  rm -f $DATADIR$dummy_EOS_citi;
  bash run_fct_data_taking.sh $sn $bl1 $bl2;
  bash run_fct_analysis.sh $sn $bl1 $bl2;
  exit
fi



########### To measure execution time #####
end=`date +%s`
###########################################
# mins=(`expr $end - $start`)/60
# secs=(`expr $end - $start`)%60

# echo 
# echo 
# echo Execution time was  and  seconds.




