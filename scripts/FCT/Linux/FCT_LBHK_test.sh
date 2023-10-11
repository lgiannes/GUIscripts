wait_LBHK_part1(){
    endline=" "
    #wait until the last line in the text file passed from user contains the desired string
    while [[ ($endline != "FEB-trig-OD LoopBack: TEST SUCCESSFUL") && ($endline != "FEB-trig-OD LoopBack test FAILED") ]]
    do
        # grab name of the last written file in the directory
        file_path_name=$( ls -tp $1 | grep -v / | head -n1 )
        #echo "file_path_name: "$file_path_name
        #if there are no files, sleep 1 and continue
        [ -z "$file_path_name" ] && sleep 1 &&  continue
        #update last line of the txt file to be compared with templates
        endline=$( tail -n 1 $1$file_path_name )
        #echo "endline: "endline
        sleep 1.5
        if [[ $endline == "ERROR: WRONG RESISTOR ON CURRENT LIMITER DETECTED." ]]
        then
            #echo "Sync.RunScript(\"$FCT_UTILS//ErrorMessage.cs\")"
            echo "-----------------------------------------------"
            echo "| PROBLEM ON CURRENT LIMITER CIRCUIT DETECTED |"
            echo "-----------------------------------------------"
            echo 
            sleep 1
            return 1
            # while (true)
            # do
            #     sleep 1
            # done
        fi
        
    done
    sleep 1
    return 0
}







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


# PART 1
# SET UP POWER SUPPLY
echo "V1 0" > /dev/ttyACM1
echo "V2 10.0" > /dev/ttyACM1
echo "OP1 0" > /dev/ttyACM1
echo "OP2 1" > /dev/ttyACM1
# echo "OP1 0" > /dev/ttyACM1


echo "/----------------------------------------------------\\"
echo "|       Move Jumper J13 to position 2-3.             |"
echo "|              Set input HV to 10 V.                 |"
echo "|                                                    |"
echo "|                   Press enter.                     |"
echo "\----------------------------------------------------/"
read -n 1
command="Sync.RunScriptArgs(\"$FCT_UTILS//LBHK_fromscript_part1.cs\",$sn)"

# chmod 777 $FCT_UTILS/wait_LBHK_part1.sh
{ sleep 1; echo $command; sleep 3; wait_LBHK_part1 $Data_path/IO_TEST/;} | telnet $ip_address $port

wait_LBHK_part1 $Data_path/IO_TEST/
wait_res=$?
# echo "wait_res: $wait_res"

if [[ $wait_res == 1 ]]
then
    # turn off HV and abort
    echo "V2 0" > /dev/ttyACM1
    echo "OP2 0" > /dev/ttyACM1
    echo "OP1 0" > /dev/ttyACM1

    return 1    
else
    # PART 2:
    # SET UP POWER SUPPLY
    echo "V1 35" > /dev/ttyACM1
    echo "V2 25" > /dev/ttyACM1
    echo "OP1 1" > /dev/ttyACM1
    echo "OP2 1" > /dev/ttyACM1
fi

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

# SET UP POWER SUPPLY
# echo "V2 0.0" > /dev/ttyACM1
# echo "OP2 0" > /dev/ttyACM1


# run ShowResults manually, only when the script is launched as standalone
# bash $FCT_RUN_FOLDER/ShowResults.sh $sn

return 0;