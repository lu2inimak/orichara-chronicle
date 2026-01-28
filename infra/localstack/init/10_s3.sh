#!/usr/bin/env sh
set -eu

: "${AWS_ENDPOINT_URL:?AWS_ENDPOINT_URL is required}"
: "${AWS_DEFAULT_REGION:=ap-northeast-1}"
: "${S3_BUCKET:=oc-media}"

echo "[init-aws:s3] endpoint=${AWS_ENDPOINT_URL} region=${AWS_DEFAULT_REGION}"
echo "[init-aws:s3] bucket=${S3_BUCKET}"

if aws --endpoint-url="${AWS_ENDPOINT_URL}" --region "${AWS_DEFAULT_REGION}" \
  s3api head-bucket --bucket "${S3_BUCKET}" >/dev/null 2>&1; then
  echo "[init-aws:s3] bucket already exists: ${S3_BUCKET}"
else
  echo "[init-aws:s3] creating bucket: ${S3_BUCKET}"
  if [ "${AWS_DEFAULT_REGION}" = "us-east-1" ]; then
    aws --endpoint-url="${AWS_ENDPOINT_URL}" --region "${AWS_DEFAULT_REGION}" \
      s3api create-bucket --bucket "${S3_BUCKET}" >/dev/null
  else
    aws --endpoint-url="${AWS_ENDPOINT_URL}" --region "${AWS_DEFAULT_REGION}" \
      s3api create-bucket --bucket "${S3_BUCKET}" \
      --create-bucket-configuration LocationConstraint="${AWS_DEFAULT_REGION}" >/dev/null
  fi
fi

echo "[init-aws:s3] s3 buckets:"
aws --endpoint-url="${AWS_ENDPOINT_URL}" --region "${AWS_DEFAULT_REGION}" \
  s3api list-buckets --query 'Buckets[].Name' --output text || true