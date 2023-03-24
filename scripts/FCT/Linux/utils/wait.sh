file_path_name=$1
# Wait until the script has finished (when the "EndOfScript.txt" file has been created)
while [ ! -f $file_path_name ]
do
sleep 1
done
# echo "EOS file found!"
sleep 1