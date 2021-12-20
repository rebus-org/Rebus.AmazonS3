# Changelog

## 1.0.0
* Initial version - thanks [micdah]
* Move configuration extensions into `Rebus.Config` namespace to help non-R# users, who are to be considered friends anyway
* Various improvements - thanks [alexanderyaremchuk]

## 1.1.0
* Add configuration option to disable updating "last read time" of data bus attachments. Can be used to reduce the number of requests to S3, so if your application is read attachments a lot, then this is recommended.

## 2.0.0
* Update to Rebus 6

## 2.1.0
* Add ability to omit awscredentials and rely integrated auth instead - thanks [ovd-capturi]

---

[alexanderyaremchuk]: https://github.com/alexanderyaremchuk
[micdah]: https://github.com/micdah
[ovd-capturi]: https://github.com/ovd-capturi