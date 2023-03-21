sn=$1

source setup.sh

GUI_path=$GUI_FOLDER
GUI_exe="/UnigeGpioBoard.exe"



Data_path=$GENERALDATADIR/FEBs/SN_$sn/
[ ! -d $Data_path ] && mkdir $Data_path
[ ! -d $Data_path/IO_TEST/ ] && mkdir $Data_path/IO_TEST/
sudo chmod 777 $Data_path/IO_TEST/ 

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


# PART 1
echo "/----------------------------------------------------\\"
echo "|       Move Jumper J13 to position 2-3.             |"
echo "|              Set input HV to 10 V.                 |"
echo "|                                                    |"
echo "|                   Press enter.                     |"
echo "\----------------------------------------------------/"
read -n 1
command="Sync.RunScriptArgs(\"$FCT_RUN_FOLDER/LBHK_fromscript_part1.cs\",$sn)"
# Open the serial com and send the command. Wait for it to end. Send second command, wait for it to end an close serial port com
{ sleep 1; echo $command; sleep 4; bash wait_LBHK_part1.sh $Data_path/IO_TEST/; } | telnet $ip_address $port 

if [[ R150K==0 ]]
then
    echo "Abort"
    exit
fi
# PART 2:
echo
echo "/----------------------------------------------------\\"
echo "|       Move Jumper J13 to position 1-2.             |"
echo "|              Set input HV to 55 V.                 |"
echo "|                                                    |"
echo "|                   Press enter.                     |"
echo "\----------------------------------------------------/"
read -n 1
command="Sync.RunScriptArgs(\"$FCT_RUN_FOLDER/LBHK_fromscript_part2.cs\",$sn)"
# Open the serial com and send the command. Wait for it to end. Send second command, wait for it to end an close serial port com
{ sleep 1; echo $command; bash wait_LBHK.sh $Data_path/IO_TEST/; } | telnet $ip_address $port 


