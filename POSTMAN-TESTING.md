# Postman Testing Guide - Product Image Upload

## Setup

1. **Start your API locally:**

   ```powershell
   cd C:\Users\berry\repos\Api
   dotnet run
   ```

   Note the port (e.g., `https://localhost:5186`)

2. **Get your JWT token** from your React app (see instructions in SETUP-BLOB-STORAGE.md)

## Postman Requests

### 1. Get All Products

```
Method: GET
URL: https://localhost:5186/api/products
Headers:
  Authorization: Bearer YOUR_JWT_TOKEN
```

### 2. Create a Product

```
Method: POST
URL: https://localhost:5186/api/products
Headers:
  Authorization: Bearer YOUR_JWT_TOKEN
  Content-Type: application/json
Body (raw JSON):
{
  "userId": 1,
  "name": "Test Product",
  "price": 29.99
}
```

**Response:** Copy the `id` and `userId` for next steps

### 3. Upload Product Image ⭐

**IMPORTANT:** The URL format is: `/api/products/{userId}/{id}/image`

```
Method: POST
URL: https://localhost:5186/api/products/1/YOUR-PRODUCT-ID/image
Headers:
  Authorization: Bearer YOUR_JWT_TOKEN
Body:
  Type: form-data
  Key: image (change type to "File")
  Value: Select your image file
```

**Example URL:**

```
https://localhost:5186/api/products/1/5664110-cc80-4cd0-9698-acdb655bcc13/image
```

**Successful Response:**

```json
{
  "imageUrl": "https://storage0404.blob.core.windows.net/product-images/guid-filename.jpg"
}
```

### 4. Get Product with Image

```
Method: GET
URL: https://localhost:5186/api/products/1/YOUR-PRODUCT-ID
Headers:
  Authorization: Bearer YOUR_JWT_TOKEN
```

**Response should include:**

```json
{
  "id": "5664110-cc80-4cd0-9698-acdb655bcc13",
  "userId": 1,
  "name": "Test Product",
  "price": 29.99,
  "imageUrl": "https://storage0404.blob.core.windows.net/product-images/abc123-image.jpg"
}
```

### 5. Delete Product Image

```
Method: DELETE
URL: https://localhost:5186/api/products/1/YOUR-PRODUCT-ID/image
Headers:
  Authorization: Bearer YOUR_JWT_TOKEN
```

## Troubleshooting

### Error: 404 Not Found from Cosmos DB

- **Cause:** Wrong userId or product doesn't exist
- **Fix:** Verify the product exists with the correct userId using GET /api/products

### Error: 400 Bad Request - "No image file provided"

- **Cause:** Image not attached or wrong form field name
- **Fix:** Ensure form-data key is exactly "image" (lowercase) and type is set to "File"

### Error: 400 Bad Request - "Invalid image type"

- **Cause:** File type not supported
- **Fix:** Use JPG, PNG, GIF, or WebP files only

### Error: 401 Unauthorized

- **Cause:** Missing or expired JWT token
- **Fix:** Get a fresh token from your React app

### SSL Certificate Error

- If Postman shows SSL certificate errors:
  - Go to Settings → General
  - Turn OFF "SSL certificate verification" for local testing

## Testing Image Display

After successful upload, copy the `imageUrl` and:

1. Paste it directly in your browser - the image should load
2. Check your React app at http://localhost:5173 - images should display in product list

## Key Changes Made

Your API now uses **userId as a partition key** throughout:

- All endpoints now include `{userId}` in the route
- This matches your Cosmos DB container configuration
- Format: `/api/products/{userId}/{id}` instead of just `/api/products/{id}`

This fixes the 404 "NotFound" errors you were seeing!
