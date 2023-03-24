# picks up the latest written file in the folder given as argument
file_path_name=$( ls -tp $1 | grep -v / | head -n1 )

# Wait until the last line of the file contains ********** or //////////
endline=false
while [[ ($endline != "/////////////////////") && ($endline != "|||||||||||||||||||||") ]]
do
    endline=$( tail -n1  $1$file_path_name )
    # echo $endline
    sleep 1
    # echo z
done
sleep 1