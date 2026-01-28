#!/usr/bin/env sh
set -eu

: "${AWS_ENDPOINT_URL:?AWS_ENDPOINT_URL is required}"
: "${AWS_DEFAULT_REGION:=ap-northeast-1}"
: "${DYNAMODB_TABLE:=oc-main}"

echo "[init-aws:dynamodb] endpoint=${AWS_ENDPOINT_URL} region=${AWS_DEFAULT_REGION}"
echo "[init-aws:dynamodb] table=${DYNAMODB_TABLE}"

if aws --endpoint-url="${AWS_ENDPOINT_URL}" --region "${AWS_DEFAULT_REGION}" \
  dynamodb describe-table --table-name "${DYNAMODB_TABLE}" >/dev/null 2>&1; then
  echo "[init-aws:dynamodb] table already exists: ${DYNAMODB_TABLE}"
else
  echo "[init-aws:dynamodb] creating table: ${DYNAMODB_TABLE}"
  aws --endpoint-url="${AWS_ENDPOINT_URL}" --region "${AWS_DEFAULT_REGION}" \
    dynamodb create-table \
      --table-name "${DYNAMODB_TABLE}" \
      --attribute-definitions \
        AttributeName=pk,AttributeType=S \
        AttributeName=sk,AttributeType=S \
      --key-schema \
        AttributeName=pk,KeyType=HASH \
        AttributeName=sk,KeyType=RANGE \
      --billing-mode PAY_PER_REQUEST \
    >/dev/null
fi

echo "[init-aws:dynamodb] dynamodb tables:"
aws --endpoint-url="${AWS_ENDPOINT_URL}" --region "${AWS_DEFAULT_REGION}" \
  dynamodb list-tables --query 'TableNames' --output text || true