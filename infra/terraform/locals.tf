locals {
  name_prefix = "${var.project_name}-${var.environment}"
  lambda_name = "${local.name_prefix}-api"
  api_name    = "${local.name_prefix}-http-api"
  pool_name   = "${local.name_prefix}-user-pool"
}
