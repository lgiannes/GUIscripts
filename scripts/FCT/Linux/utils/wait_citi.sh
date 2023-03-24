dummy_EOS="EndOfScript_citi.txt"
SN_="SN_"

sn=$1

Data_path=$DATADIR
# echo $DATADIR

# Wait until the script has finished (when the "EndOfScript.txt" file has been created)
# echo "Waiting for end of data taking ... (see GUI socket window for status)"
while [ ! -f $Data_path$dummy_EOS ]
do
sleep 1
done
sleep 1