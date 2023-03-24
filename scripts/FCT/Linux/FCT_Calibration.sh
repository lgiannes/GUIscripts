if [[ -z $1 ]]
then
echo Tell me the Serial Number! Thank you
exit
fi

sn=$1

source $FCT_RUN_FOLDER/setup.sh

GUI_path=$GUI_FOLDER
GUI_exe="/UnigeGpioBoard.exe"


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

# run analysis
exe_path=$ANALYSIS_FOLDER"/bin/";
exe_calib="CalibrationPlots"
$exe_path$exe_calib -s $sn;