SN=$1

source setup.sh

DATADIR=$GENERALDATADIR/FEBs/SN_$SN
echo
tail -n3 $DATADIR/output_*.txt
LBHK_dir=$DATADIR/IO_TEST/
IO_TEST_output=$( ls $LBHK_dir -tp | grep "IO_TEST_" | grep -v / | head -n1 )

echo
echo "==>" $DATADIR/IO_TEST/$IO_TEST_output "<=="

tail -n3 $DATADIR/IO_TEST/$IO_TEST_output
echo