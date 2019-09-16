isActive() {
   local status=$(awslocal kinesis describe-stream --stream-name ${KINESIS_STREAM_NAME} | jq '.StreamDescription.StreamStatus')
   if [ $status == 'Active' ]; then return 1; fi
   return 0
}

while ! isActive; do
   sleep 5
done