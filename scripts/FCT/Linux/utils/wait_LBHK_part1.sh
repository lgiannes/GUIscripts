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
        echo "Sync.RunScript(\"$FCT_UTILS//ErrorMessage.cs\")"
        sleep 1
        return -1
        # while (true)
        # do
        #     sleep 1
        # done
    fi
    
done
sleep 1
