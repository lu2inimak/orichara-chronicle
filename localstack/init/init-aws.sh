#!/usr/bin/env sh
set -eu

: "${AWS_ENDPOINT_URL:?AWS_ENDPOINT_URL is required}"
: "${AWS_DEFAULT_REGION:=ap-northeast-1}"
: "${S3_BUCKET:=oc-media}"
: "${DYNAMODB_TABLE:=oc-main}"

echo "[init-aws] endpoint=${AWS_ENDPOINT_URL} region=${AWS_DEFAULT_REGION}"
echo "[init-aws] bucket=${S3_BUCKET} table=${DYNAMODB_TABLE}"

# ---- S3 bucket ----
if [ "${AWS_DEFAULT_REGION}" = "us-east-1" ]; then
  aws --endpoint-url="${AWS_ENDPOINT_URL}" --region "${AWS_DEFAULT_REGION}" \
    s3api create-bucket --bucket "${S3_BUCKET}"
else
  aws --endpoint-url="${AWS_ENDPOINT_URL}" --region "${AWS_DEFAULT_REGION}" \
    s3api create-bucket --bucket "${S3_BUCKET}" \
    --create-bucket-configuration LocationConstraint="${AWS_DEFAULT_REGION}"
fi

# 確認
echo "[init-aws] s3 buckets:"
aws --endpoint-url="${AWS_ENDPOINT_URL}" --region "${AWS_DEFAULT_REGION}" \
  s3api list-buckets --query 'Buckets[].Name' --output text || true

# ---- DynamoDB table ----
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
  >/dev/null 2>&1 || true

# 確認
echo "[init-aws] dynamodb tables:"
aws --endpoint-url="${AWS_ENDPOINT_URL}" --region "${AWS_DEFAULT_REGION}" \
  dynamodb list-tables --query 'TableNames' --output text || true