#!/usr/bin/env bash

KINESIS_STREAM_SHARDS=${KINESIS_STREAM_SHARDS:-1}

awslocal kinesis create-stream --shard-count ${KINESIS_STREAM_SHARDS} \
  --stream-name ${KINESIS_STREAM_NAME}

        
/scripts/waitForActive.sh

awslocal kinesis split-shard --stream-name ${KINESIS_STREAM_NAME} --shard-to-split shardId-000000000003 --new-starting-hash-key 238197656844656924424362225202237748018

/scripts/waitForActive.sh

#awslocal kinesis merge-shards --stream-name reader-stream --shard-to-merge shardId-000000000004 --adjacent-shard-to-merge shardId-000000000005

awslocal kinesis describe-stream --stream-name ${KINESIS_STREAM_NAME}

