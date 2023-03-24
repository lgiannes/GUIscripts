sn=$1
bl1=$2
bl2=$3

if [[ -z $4 ]]
then
    str_input="YESCALIB"
else
    str_input=$4
fi

GUI_path=$GUI_FOLDER
GUI_exe="/UnigeGpioBoard.exe"
# Set the ip address of this machine and the port set for the GUI


dummy_EOS="EndOfScript.txt"
dummy_EOS_citi="EndOfScript_citi.txt"
Data_path=$DATADIR
echo "DATADIR: "$DATADIR

# Define the command to run the GUI script
command="Sync.RunScriptArgs(\"$FCT_UTILS/Script_FCT_openshort.cs\",$sn,$bl1,$bl2)"
command_citi="Sync.RunScriptArgs(\"$FCT_UTILS/Script_FCT_CITI_test.cs\",$sn)"
#debug
#echo "in:  "$str_input
#echo "cmp: "$str_cal

if [[ "$str_input" = "$str_cal" ]]
then
  echo
  echo "Not doing calibration."
  echo
  command_merged="Sync.RunScriptArgs(\"$FCT_UTILS/Script_FCT_merged_NOCALIB.cs\",$sn,$bl1,$bl2)"
else
  echo
  echo "Calibration will be performed."
  echo
  command_merged="Sync.RunScriptArgs(\"$FCT_UTILS/Script_FCT_merged.cs\",$sn,$bl1,$bl2)"
fi
# Opens GUI only if there are no GUI already open
if [ -z $(pidof mono) ]
then 
( cd $GUI_path && mono $GUI_path$GUI_exe & )
echo "Opening GUI ..."
sleep 0.5
echo "When GUI is open, press Enter "
echo "(Close pop-up error windows on GUI, if any. DO NOT CLOSE THE SOCKET WINDOW! )"
read -n 1
fi

# Open the serial com and send the command. Wait for it to end. Send second command, wait for it to end an close serial port com
{ sleep 1; echo $command_merged; bash $FCT_UTILS/wait.sh $Data_path$dummy_EOS; } | telnet $ip_address $port 
# sleep 2
# { sleep 1; echo $command_citi; bash wait.sh $Data_path$dummy_EOS_citi; } | telnet $ip_address $port 

# sleep 1; echo $command; bash wait.sh $Data_path$dummy_EOS;

# Close the GUI
if [ -z $(pidof mono) ]
then 
    echo
else
    sudo kill $(pidof mono)
fi