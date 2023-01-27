# picks up the latest written file in the folder given as argument
file_path_name=$( ls -tp $1 | grep -v / | head -n1 )

# Wait until the last line of the file contains teh desired string
endline=false
while [[ ($endline != "FEB-trig-OD LoopBack: TEST SUCCESSFUL") && ($endline != "FEB-trig-OD LoopBack test FAILED") ]]
do
    endline=$( tail -n 1 $1$file_path_name )
    # echo $endline
    sleep 1
    # echo z
done
sleep 1