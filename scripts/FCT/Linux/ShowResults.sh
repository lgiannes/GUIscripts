SN=$1

source setup.sh

DATADIR=$GENERALDATADIR/SN_$SN/
echo
tail -n3 $DATADIR/output_*.txt
IO_TEST_output=$( ls $DATADIR/IO_TEST/ -tp | grep "IO_TEST" | grep -v / | head -n1 )
echo
echo "==>" $DATADIR/IO_TEST/$IO_TEST_output "<=="

tail -n3 $DATADIR/IO_TEST/$IO_TEST_output
echo