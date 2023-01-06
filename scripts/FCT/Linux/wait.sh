Data_path="/home/neutrino/FCT/data_local/";
dummy_EOS="EndOfScript.txt"
SN_="SN_"

sn=$1

Data_path=$Data_path$SN_$sn/

# Wait until the script has finished (when the "EndOfScript.txt" file has been created)
# echo "Waiting for end of data taking ... (see GUI socket window for status)"
while [ ! -f $Data_path$dummy_EOS ]
do
sleep 1
done
