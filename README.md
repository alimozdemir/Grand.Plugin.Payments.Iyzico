# GrandNode Payment Iyzico

This repository contains an implementation of [Iyzico](https://www.iyzico.com/) Payment System on [GrandNode](https://github.com/grandnode/grandnode). Iyzico has a [dotnet](https://github.com/iyzico/iyzipay-dotnet) API which does not support .net core yet. Therefore, I had to use unoffical version of the API which is implemented by armut.com (it is a popular website in Turkey.) You can take a look into the armut's repository [here](https://github.com/armutcom/iyzipay-dotnet-client).

## Workflow

The plugin first tries to process as regular payment (`ProcessPayment`). If the card requires 3d secure payment (Error `DEBIT_CARDS_REQUIRES_3DS`) the plugin redirects the user to 3d secure payment method (`PostProcessPayment`). Thus, the payment completes as success or failure. On the failure, the plugin adds a order note that only admins can see.

### 3D secure

I have added a custom value on the payment form that 3d secure payment method. If the customer selects that checkbox the plugin redirects the user's browser to `PaymentIyzicoController`.`ThreeDPayment` Action and shows the html content of iyzico api result. Then `PaymentIyzicoController`.`Handler` handle the rest of the payment. 


## Note for the developers and GrandNode owners.

Iyzico has a different approach than regular payment system. It requires order information at the beginning (`ProcessPayment`) but GrandNode does not supply those information at that method. For that, I will update my local version which I won't going to share any soon but you can do it yourself and open an/a issue/PR to GrandNode repository. You can start with change the method signatures on `IPaymentMethod` interface.


