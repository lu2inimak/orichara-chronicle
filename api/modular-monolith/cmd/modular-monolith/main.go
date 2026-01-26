package main

import (
	"context"
	"fmt"
	"log"
	"net/http"
	"os"
	"time"

	"github.com/aws/aws-sdk-go-v2/config"
	"github.com/aws/aws-sdk-go-v2/service/dynamodb"
	"github.com/aws/aws-sdk-go-v2/service/s3"
)

func main() {
	ctx := context.Background()

	awsEndpoint := os.Getenv("AWS_ENDPOINT_URL")

	cfg, err := config.LoadDefaultConfig(ctx,
		config.WithRegion(os.Getenv("AWS_DEFAULT_REGION")),
	)
	if err != nil {
		log.Fatalf("aws config error: %v", err)
	}

	// LocalStack用 endpoint override
	if awsEndpoint != "" {
		cfg.BaseEndpoint = &awsEndpoint
	}

	dynamo := dynamodb.NewFromConfig(cfg)
	s3client := s3.NewFromConfig(cfg)

	http.HandleFunc("/health", func(w http.ResponseWriter, r *http.Request) {
		fmt.Fprintln(w, "ok")
	})

	http.HandleFunc("/aws-check", func(w http.ResponseWriter, r *http.Request) {
		ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
		defer cancel()

		dout, derr := dynamo.ListTables(ctx, &dynamodb.ListTablesInput{})
		sout, serr := s3client.ListBuckets(ctx, &s3.ListBucketsInput{})

		fmt.Fprintf(w, "Dynamo err: %v\n", derr)
		if derr == nil {
			fmt.Fprintf(w, "Dynamo tables: %v\n", dout.TableNames)
		}

		fmt.Fprintf(w, "\nS3 err: %v\n", serr)
		if serr == nil {
			names := make([]string, 0, len(sout.Buckets))
			for _, b := range sout.Buckets {
				if b.Name != nil {
					names = append(names, *b.Name)
				}
			}
			fmt.Fprintf(w, "S3 buckets: %v\n", names)
		}
	})

	port := "8080"
	log.Printf("starting on :%s", port)
	log.Fatal(http.ListenAndServe(":"+port, nil))
}
