if [[ -z $1 ]]
then
echo Tell me the Serial Number! Thank you
exit
fi

sn=$1

source $FCT_RUN_FOLDER/setup.sh

GUI_path=$GUI_FOLDER
GUI_exe="/UnigeGpioBoard.exe"

# activate HV
# version setup2
  echo "V1 35" > /dev/ttyACM1
  echo "V2 25" > /dev/ttyACM1
  echo "OP2 1" > /dev/ttyACM1
  echo "OP1 1" > /dev/ttyACM1


dummy_EOS="EndOfCalib.txt"
Data_path=$GENERALDATADIR/FEBs/SN_$sn/
[ ! -d $Data_path ] && mkdir $Data_path
[ ! -d $Data_path/Calibration ] && mkdir $Data_path/Calibration
sudo chmod 777 $Data_path/Calibration/ 

# Close all GUIs to avoid double serial com
if [ -z $(pidof mono) ]
then 
    ( cd $GUI_path && mono $GUI_path$GUI_exe& )
    echo "Opening GUI ..."
    sleep 0.5
    echo
    echo "When GUI is open, press Enter "
    echo "(Close pop-up error windows on GUI, if any. DO NOT CLOSE THE SOCKET WINDOW! )"
    read -n 1
else
    sudo kill $(pidof mono)
    ( cd $GUI_path && mono $GUI_path$GUI_exe& )
    echo "Opening GUI ..."
    sleep 0.5
    echo
    echo "When GUI is open, press Enter "
    echo "(Close pop-up error windows on GUI, if any. DO NOT CLOSE THE SOCKET WINDOW! )"
    read -n 1
fi

if [[ -f $Data_path$dummy_EOS ]]
then
    rm -f $Data_path$dummy_EOS
fi
command="Sync.RunScriptArgs(\"$FCT_UTILS/Script_FCT_merged.cs\",$sn,0,0,true)"
{ sleep 1; echo $command; bash $FCT_UTILS/wait.sh $Data_path$dummy_EOS; } | telnet $ip_address $port 

# deactivate HV
# version setup2
  echo "V2 0" > /dev/ttyACM1
  echo "V1 0" > /dev/ttyACM1
  echo "OP1 0" > /dev/ttyACM1
  echo "OP2 0" > /dev/ttyACM1


# run analysis
exe_path=$ANALYSIS_FOLDER"/bin/";
exe_calib="CalibrationPlots"
$exe_path$exe_calib -s $sn;


# close GUI
sudo kill $(pidof mono)
echo 