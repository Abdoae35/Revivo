# Automatic car images

Revivo uses the official Unsplash photo search API for an illustrative car photo. Create an Unsplash developer application and store its **Access Key** in the API project's .NET User Secrets. In `zsh`, enter it without placing the value in shell history:

```sh
read -s "REVIVO_UNSPLASH_KEY?Unsplash Access Key: "
dotnet user-secrets set "Unsplash:AccessKey" "$REVIVO_UNSPLASH_KEY" --project Revivo.Api
unset REVIVO_UNSPLASH_KEY
```

Call `GET /api/car-catalog/car-image?modelId={id}&year={year}` with an existing model ID and a valid year. The API resolves the make and model from RevivoDb and searches for a relevant landscape photo. A `found` response includes the hotlinked `imageUrl`, `photographerName`, `photographerUrl`, `photoUrl`, and `unsplashUrl`. The `isExactVehicle` field is always `false`: search results are illustrative and do not verify that the pictured car is the selected model or year.

For `noImage`, `rateLimited`, `notConfigured`, or `unavailable`, the API returns HTTP 200 with a `null` image URL and a message so the client can display a bundled placeholder. An unknown model returns 404; an invalid year returns 400. Successful searches are cached for six hours, empty searches for 15 minutes, and rate-limit responses for one minute in process memory. Other transient errors are not cached.

Flutter can call this endpoint when a vehicle's existing `imageUrl` is empty, using the vehicle's `carModelId` and `year`. Keep the returned attribution alongside the photo. Do not save just the Unsplash URL into `Vehicle.ImageUrl`, because that field does not store the required attribution. Existing vehicle CRUD and database schema are unchanged.

When displaying a photo, show “Photo by {photographerName} on Unsplash” with links to `photographerUrl` and `unsplashUrl`. Use `imageUrl` directly rather than downloading or proxying it. These URLs come from Unsplash's API and include its tracking identifier. Unsplash's [API documentation](https://unsplash.com/documentation), [attribution guideline](https://help.unsplash.com/en/articles/2511315-guideline-attribution), and [hotlinking guideline](https://help.unsplash.com/en/articles/2511271-guideline-hotlinking-images) describe the requirements.
