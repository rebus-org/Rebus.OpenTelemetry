# Changelog

## 0.0.1
* Initial version made by [zlepper] based on work from [dariogriffo]


## 0.0.2
* Fix baggage not being propagated across the wire - thanks [zlepper]

## 0.0.3
* Fix activity context not being available in logger - thanks [zlepper]

## 0.0.4
*  Make activity kind be "internal" for the process step - thanks [zlepper]

## 0.0.5
* Update packages and framework targets - thanks [arildboifot]

## 1.0.0
* Update to Rebus 8
* Add system.diagnostics.metrics.meter support - thanks [droosma]
* Rebus hard dependency on Rebus' intent header - thanks [riezebosch]

## 1.0.1
* Prevent tag duplicates - thanks [pfab-io]

## 1.1.0
* Extend System.Diagnostics.DiagnosticSource version range to work with .NET 9 - thanks [rasmusjp]

## 1.1.1
* Fix inverted message size and message delay meters - thanks [arielmoraes]

## 1.2.0
* Forward exceptions and add a bit more data to traces - thanks [zlepper]

[arielmoraes]: https://github.com/arielmoraes
[arildboifot]: https://github.com/arildboifot
[dariogriffo]: https://github.com/dariogriffo
[droosma]: https://github.com/droosma
[rasmusjp]: https://github.com/rasmusjp
[riezebosch]: https://github.com/riezebosch
[pfab-io]: https://github.com/pfab-io
[zlepper]: https://github.com/zlepper
