# Car catalog source and import

Source: [gor3a/vehicle-makes-models](https://github.com/gor3a/vehicle-makes-models), using `data/csv/makes-models.csv`. The project's published coverage is 164 makes, 2,621 models, 7,169 generations and 30,390 engine variants. Revivo imports only make, model, and optional model year bounds. The source notes Egypt-market additions including Speranza and El Nasr; import includes all source makes and models so those are retained.

The dataset is licensed under [ODbL 1.0](https://github.com/gor3a/vehicle-makes-models/blob/main/LICENSE-DATA). Credit the vehicle-makes-models project and autoevolution.com in any public catalog use. The source's data license also describes share-alike and public availability obligations for adapted databases. Revivo does not bundle a copy of the source CSV in this repository.

Download the published CSV on the development machine, review it, apply the `AddCarModelYears` migration, then run the importer against the configured `RevivoDb` connection:

```sh
curl -L --fail --output /tmp/revivo-makes-models.csv https://raw.githubusercontent.com/gor3a/vehicle-makes-models/main/data/csv/makes-models.csv
dotnet ef database update --project Revivo.Infrastructure --startup-project Revivo.Api
dotnet run --project Revivo.Api -- --seed-car-catalog /tmp/revivo-makes-models.csv
```

The importer is idempotent by case-insensitive make name and `(make, model)` name. It preserves existing rows and fills only missing year bounds. A serializable transaction protects each run. Rows with malformed names or year values stop the import before database changes. Run the command again for updated source data; it does not delete existing records. The source CSV's `year_end` may be empty for ongoing models. The years endpoint uses the current calendar year as a display limit in that case. These are model-wide bounds, not proof that every trim or engine variant existed in every returned year.
