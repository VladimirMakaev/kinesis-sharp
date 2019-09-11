#!/usr/bin/env bash

KINESIS_STREAM_SHARDS=${KINESIS_STREAM_SHARDS:-1}

awslocal kinesis create-stream --shard-count ${KINESIS_STREAM_SHARDS} \
  --stream-name ${KINESIS_STREAM_NAME}