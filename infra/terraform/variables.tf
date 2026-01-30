variable "aws_region" {
  type        = string
  description = "AWS region to deploy resources into."
}

variable "project_name" {
  type        = string
  description = "Project name prefix for resource naming."
  default     = "orichara-chronicle"
}

variable "environment" {
  type        = string
  description = "Environment name (e.g. dev, prod)."
  default     = "dev"
}

variable "s3_bucket_name" {
  type        = string
  description = "S3 bucket name for media storage. Must be globally unique."
}

variable "dynamodb_table_name" {
  type        = string
  description = "DynamoDB table name."
}

variable "tags" {
  type        = map(string)
  description = "Default tags applied to all resources."
  default = {
    project = "orichara-chronicle"
  }
}
