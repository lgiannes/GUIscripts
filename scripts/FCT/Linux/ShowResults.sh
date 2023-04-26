if [[ -z $1 ]]
then
echo Tell me the Serial Number! Thank you
exit
fi

sn=$1

source $FCT_RUN_FOLDER/setup.sh

DATADIR=$GENERALDATADIR/FEBs/SN_$sn
echo
tail -n3 $DATADIR/output_*.txt
LBHK_dir=$DATADIR/IO_TEST/
IO_TEST_output=$( ls $LBHK_dir -tp | grep "IO_TEST_" | grep -v / | head -n1 )

echo
echo "==>" $DATADIR/IO_TEST/$IO_TEST_output "<=="

tail -n3 $DATADIR/IO_TEST/$IO_TEST_output
echo


if [ ! -f  "$DATADIR/Summary/ADC_data_SN$sn.csv" ]
then 
echo "ADC summary file NOT correctly stored"
else
echo "$DATADIR/Summary/ADC_data_SN$sn.csv"
fi