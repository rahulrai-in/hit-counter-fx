# Visitor Counter Badge (Free!)

> _Powered by Microsoft Azure_

<p align="center">
  <a href="#">
    <img src="https://badge.tcblab.net/api/hitcounter/rahul/badge"/>
  </a>
</p>

Visitor Counter Badge is a simple open-source utility you can use to display the number of visitors on a web page, repository, or profile. Every request to render the visitor count badge invokes an HTTP-triggered Azure function that dynamically generates an SVG image that you can apply on a web page, profile page, or repository. You can host this service on your Azure subscription by using the [ARM deployment button](#self-hosting) below.

If you are further interested in learning the internals of this service, please read the [Visitor Counter Badge article on my blog](https://thecloudblog.net/lab/serverless-visitor-counter-badge-with-azure-functions/). While you are there, please consider subscribing to my mailing list to receive updates on new articles on Cloud-Native and Kubernetes.

## Get in touch

[![](https://img.shields.io/twitter/follow/rahulrai_in?color=blue&label=tweet&logo=twitter&logoColor=white&style=flat-square)](https://twitter.com/rahulrai_in) [![](https://img.shields.io/badge/blog-subscribe-blue?style=flat-square&logo=rss&labelColor=gray&color=blue&logoColor=white&cacheSeconds=3600)](https://thecloudblog.net/)

I love reading tweets. Send me your tweets, or connect with me via my blog - The Cloud Blog!

## Appeal

![](https://img.shields.io/github/stars/rahulrai-in/hit-counter-fx?style=flat-square) [![](https://img.shields.io/badge/paypal-donate-blue?style=flat-square&logo=paypal&labelColor=orange&color=blue&cacheSeconds=3600)](https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=NYDG9PGQ8KD8N&item_name=Thanks+for+supporting+open-source+software+and+being+an+A1+member+of+the+community&currency_code=AUD) ![](https://img.shields.io/badge/-no%20misuse-gray?style=flat-square)

1. If you are using this badge somewhere or want to show your appreciation, **please add a ⭐ to the repository**.
2. This function runs on the Y1-Dynamic App Service Plan (a.k.a. Consumption plan) on a Pay-As-You-Go (PAYG) subscription. You can help me make this service better for everyone by [**making a small contribution on PayPal**](https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=NYDG9PGQ8KD8N&item_name=Thanks+for+supporting+open-source+software+and+being+an+A1+member+of+the+community&currency_code=AUD) or sponsoring me on GitHub (coming soon). Thank you for your support!
3. Please **do not misuse this service**. Consider hosting it yourself if you want to experiment or anticipate huge traffic. If you spot any bugs, please create an issue. I welcome your pull requests.

## Step 1: Register a username

To use this badge, you first need to register a username. To do that, use an HTTP client of your choice, such as cURL or POSTMAN, to make a POST request to the following endpoint.

```sh
curl -X POST -d "" 'https://badge.tcblab.net/api/hitcounter/[Your Username]'
```

> **Note**: Your username must not be longer than 10 characters in length and must only contain alphanumeric characters.

This request will return either of the following two responses.

| HTTP response | Definition | Operation Status                                  |
| ------------- | ---------- | ------------------------------------------------- |
| 200           | OK         | Username successfully registered                  |
| 401           | CONFLICT   | Username already exists. Choose another username. |

## Step 2: Apply the badge

A page is uniquely identified through a page identifier (case insensitive) and your username. You can use any unique string to identify your page within your account. The most common choices are the title of the page, a number, or a GUID. Once you select an identifier, you can apply the badge on an HTML page, such as a blog post, using the following code.

```html
<img
  src="https://badge.tcblab.net/api/hitcounter/[Your Username]/[Page Identifier]"
/>
```

> **Note**: Your page identifier must be less than 50 characters in length and only contain alphanumeric characters, hyphens (-), and underscore (\_).

If you want to apply the badge on a markdown file such as README.md or your GitHub profile, use the following code.

```markdown
![](https://badge.tcblab.net/api/hitcounter/[Your Username]/[Page Identifier])
```

## Configurations

Most of the aspects of the badge are configurable. The configuration settings are read from the query string as follows.

```plaintext
https://badge.tcblab.net/api/hitcounter/[Your Username]/[Page Identifier]?[OptionKey1]=[Value]&[OptionKey2]=[Value]...
```

Following is the list of supported parameters. See the [raw view of the README file](/main/README.md) for the query string values used in the examples below.

| Parameter name          | Supported values                 | What it does                                                | Example                                                                                                      |
| ----------------------- | -------------------------------- | ----------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------ |
| IconBackgroundColorCode | Color name or hex code (incl. #) | Specify the background color of the label (left)            | ![](https://badge.tcblab.net/api/hitcounter/rahul/badge-demo?NoCount=true&IconBackgroundColorCode=red)       |
| EyeColorCode            | Color name or hex code (incl. #) | Specify the color of the eye icon (left)                    | ![](https://badge.tcblab.net/api/hitcounter/rahul/badge-demo?NoCount=true&EyeColorCode=%23FF00FF)            |
| TextBackgroundColorCode | Color name or hex code (incl. #) | Specify the background color of the label (right)           | ![](https://badge.tcblab.net/api/hitcounter/rahul/badge-demo?NoCount=true&TextBackgroundColorCode=%2398FB98) |
| TextColorCode           | Color name or hex code (incl. #) | Specify the color of the counter (right)                    | ![](https://badge.tcblab.net/api/hitcounter/rahul/badge-demo?NoCount=true&TextColorCode=black)               |
| NoCount                 | true (default) or false          | Display the count without incrementing the counter          | ![](https://badge.tcblab.net/api/hitcounter/rahul/badge-demo?NoCount=true&test=1)                            |
| IsKmbFormat             | true (default) or false          | Format large numbers as K(Kilo), M(Million), and B(Billion) | ![](https://badge.tcblab.net/api/hitcounter/rahul/badge-demo-kmb?NoCount=true&IsKmbFormat=false)             |

> **Note**: Do not use the symbol `#` in the value of the query string parameter. Use the URL encoded value `%23` instead.

## Self hosting

You can deploy this function to your Azure subscription and customize it to suit your needs. Execute the following [AZ CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli) command to create a resource group in your subscription.

```sh
New-AzResourceGroup -Name <resource-group-name> -Location <resource-group-location>
```

Click on the following button to create an Azure Function in the resource group you created.

[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Frahulrai-in%2Fhit-counter-fx%2Fmain%2Fazuredeploy.json)

Deploy the application code to your Azure Function. There are several ways of doing so, such as [GitHub Actions](https://github.com/marketplace/actions/azure-functions-action), [VSCode](https://docs.microsoft.com/en-us/azure/azure-functions/create-first-function-vs-code-csharp), and [Visual Studio](https://docs.microsoft.com/en-us/azure/azure-functions/functions-develop-vs).
