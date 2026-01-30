resource "local_file" "dotenv" {
  filename = "${path.module}/../../.env"

  content = <<-EOT
AWS_REGION=${var.aws_region}
AWS_DEFAULT_REGION=${var.aws_region}
S3_BUCKET=${var.s3_bucket_name}
DYNAMODB_TABLE=${var.dynamodb_table_name}
EOT
}

resource "local_file" "tfvars" {
  filename = "${path.module}/terraform.auto.tfvars.json"

  content = jsonencode({
    aws_region          = var.aws_region
    s3_bucket_name      = var.s3_bucket_name
    dynamodb_table_name = var.dynamodb_table_name
  })
}
