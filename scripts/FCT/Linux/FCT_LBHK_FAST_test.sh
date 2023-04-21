if [[ -z $1 ]]
then
echo Tell me the Serial Number! Thank you
exit
fi

sn=$1

source $FCT_RUN_FOLDER/setup.sh

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




# PART 2:
echo
echo "/----------------------------------------------------\\"
echo "|       Move Jumper J13 to position 1-2.             |"
echo "|              Set input HV to $PS_HV V.                 |"
echo "|                                                    |"
echo "|                   Press enter.                     |"
echo "\----------------------------------------------------/"
read -n 1
command="Sync.RunScriptArgs(\"$FCT_UTILS//LBHK_fromscript_part2.cs\",$sn)"
{ sleep 1; echo $command; bash $FCT_UTILS/wait_LBHK.sh $Data_path/IO_TEST/; } | telnet $ip_address $port 


# run ShowResults manually, only when the script is launched as standalone
# bash $FCT_RUN_FOLDER/ShowResults.sh $sn