<div align="center">

<img src="assets/icon.png" alt="drawing" width="700px"/></br>

[![NuGet](https://img.shields.io/nuget/v/vsaresults.svg)](https://www.nuget.org/packages/vsaresults)

[![Build](https://github.com/madhatter5501/ErrorOr/actions/workflows/ci.yml/badge.svg)](https://github.com/madhatter5501/ErrorOr/actions/workflows/ci.yml)

[![GitHub contributors](https://img.shields.io/github/contributors/madhatter5501/ErrorOr)](https://GitHub.com/madhatter5501/ErrorOr/graphs/contributors/) [![GitHub Stars](https://img.shields.io/github/stars/madhatter5501/ErrorOr.svg)](https://github.com/madhatter5501/ErrorOr/stargazers) [![GitHub license](https://img.shields.io/github/license/madhatter5501/ErrorOr)](https://github.com/madhatter5501/ErrorOr/blob/main/LICENSE)
[![codecov](https://codecov.io/gh/madhatter5501/ErrorOr/branch/main/graph/badge.svg)](https://codecov.io/gh/madhatter5501/ErrorOr)

---

## Origin / Provenance

This repository is a **modernized fork** of [ErrorOr](https://github.com/amantinband/error-or) by [Amichai Mantinband](https://github.com/amantinband).

The original project is licensed under the MIT License. This fork extends the library with additional features (.NET 10 support, new error types, JSON serialization, LINQ-style methods) while maintaining full API compatibility.

---

### A simple, fluent discriminated union of an error or a result.

**⚠️ Note:** This is a work in progress.
`dotnet add package VsaResults`

</div>

- [Give it a star ⭐!](#give-it-a-star-)
- [Quick Start](#quick-start)
- [Getting Started 🏃](#getting-started-)

  - [Replace throwing exceptions with `ErrorOr<T>`](#replace-throwing-exceptions-with-errorort)
  - [Support For Multiple Errors](#support-for-multiple-errors)
  - [Various Functional Methods and Extension Methods](#various-functional-methods-and-extension-methods)
    - [Real world example](#real-world-example)
    - [Simple Example with intermediate steps](#simple-example-with-intermediate-steps)
      - [No Failure](#no-failure)
      - [Failure](#failure)
- [Creating an `ErrorOr` instance](#creating-an-erroror-instance)
  - [Using implicit conversion](#using-implicit-conversion)
  - [Using The `ErrorOrFactory`](#using-the-errororfactory)
  - [Using The `ToErrorOr` Extension Method](#using-the-toerroror-extension-method)
- [Properties](#properties)
  - [`IsError`](#iserror)
  - [`Value`](#value)
  - [`Errors`](#errors)
  - [`FirstError`](#firsterror)
  - [`ErrorsOrEmptyList`](#errorsoremptylist)
  - [`TryGetValue`](#trygetvalue)
  - [`TryGetErrors`](#trygeterrors)
  - [`GetValueOrDefault`](#getvalueordefault)
- [Methods](#methods)
  - [`Match`](#match)
    - [`Match`](#match-1)
    - [`MatchAsync`](#matchasync)
    - [`MatchFirst`](#matchfirst)
    - [`MatchFirstAsync`](#matchfirstasync)
  - [`Switch`](#switch)
    - [`Switch`](#switch-1)
    - [`SwitchAsync`](#switchasync)
    - [`SwitchFirst`](#switchfirst)
    - [`SwitchFirstAsync`](#switchfirstasync)
  - [`Then`](#then)
    - [`Then`](#then-1)
    - [`ThenAsync`](#thenasync)
    - [`ThenDo` and `ThenDoAsync`](#thendo-and-thendoasync)
    - [Mixing `Then`, `ThenDo`, `ThenAsync`, `ThenDoAsync`](#mixing-then-thendo-thenasync-thendoasync)
  - [`FailIf`](#failif)
  - [`Else`](#else)
    - [`Else`](#else-1)
    - [`ElseAsync`](#elseasync)
- [Mixing Features (`Then`, `FailIf`, `Else`, `Switch`, `Match`)](#mixing-features-then-failif-else-switch-match)
- [Error Types](#error-types)
  - [Built in error types](#built-in-error-types)
  - [Custom error types](#custom-error-types)
- [Built in result types (`Result.Success`, ..)](#built-in-result-types-resultsuccess-)
- [Additional Methods](#additional-methods)
  - [`MapError`](#maperror)
  - [`OrElse`](#orelse)
  - [`GetValueOrThrow`](#getvalueorthrow)
  - [`Flatten`](#flatten)
  - [Tuple Deconstruction](#tuple-deconstruction)
  - [`ErrorOrUnorderedEqualityComparer`](#errororunorderedequalitycomparer)
  - [`Try` and `TryAsync`](#try-and-tryasync)
  - [`Combine` and `Collect`](#combine-and-collect)
  - [LINQ-Style Methods](#linq-style-methods)
- [Organizing Errors](#organizing-errors)
- [Feature Pipeline (VSA)](#feature-pipeline-vsa)
- [Wide Events](#wide-events)
- [Samples](#samples)
- [API Reference](#api-reference)
- [Contribution 🤲](#contribution-)
- [Credits 🙏](#credits-)
- [License 🪪](#license-)

# Give it a star ⭐!

Loving it? Show your support by giving this project a star!

# Quick Start

Install the package:

`dotnet add package VsaResults`

Create and consume an `ErrorOr<T>` in a few lines:

```cs
public static ErrorOr<int> Parse(string input)
    => int.TryParse(input, out var value)
        ? value
        : Error.Validation("Parse.Invalid", "Input must be a number");

var message = Parse("42")
    .Then(value => value * 2)
    .Match(
        value => $"Value: {value}",
        errors => errors[0].Description);

Console.WriteLine(message);
```

# Getting Started 🏃


## Replace throwing exceptions with `ErrorOr<T>`

This 👇

```cs
public float Divide(int a, int b)
{
    if (b == 0)
    {
        throw new Exception("Cannot divide by zero");
    }

    return a / b;
}

try
{
    var result = Divide(4, 2);
    Console.WriteLine(result * 2); // 4
}
catch (Exception e)
{
    Console.WriteLine(e.Message);
    return;
}
```

Turns into this 👇

```cs
public ErrorOr<float> Divide(int a, int b)
{
    if (b == 0)
    {
        return Error.Unexpected(description: "Cannot divide by zero");
    }

    return a / b;
}

var result = Divide(4, 2);

if (result.IsError)
{
    Console.WriteLine(result.FirstError.Description);
    return;
}

Console.WriteLine(result.Value * 2); // 4
```

Or, using [Then](#then--thenasync)/[Else](#else--elseasync) and [Switch](#switch--switchasync)/[Match](#match--matchasync), you can do this 👇

```cs

Divide(4, 2)
    .Then(val => val * 2)
    .SwitchFirst(
        onValue: Console.WriteLine, // 4
        onFirstError: error => Console.WriteLine(error.Description));
```

## Support For Multiple Errors

Internally, the `ErrorOr` object has a list of `Error`s, so if you have multiple errors, you don't need to compromise and have only the first one.

```cs
public class User(string _name)
{
    public static ErrorOr<User> Create(string name)
    {
        List<Error> errors = [];

        if (name.Length < 2)
        {
            errors.Add(Error.Validation(description: "Name is too short"));
        }

        if (name.Length > 100)
        {
            errors.Add(Error.Validation(description: "Name is too long"));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add(Error.Validation(description: "Name cannot be empty or whitespace only"));
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        return new User(name);
    }
}
```

## Various Functional Methods and Extension Methods

The `ErrorOr` object has a variety of methods that allow you to work with it in a functional way.

This allows you to chain methods together, and handle the result in a clean and concise way.

### Real world example

```cs
return await _userRepository.GetByIdAsync(id)
    .Then(user => user.IncrementAge()
        .Then(success => user)
        .Else(errors => Error.Unexpected("Not expected to fail")))
    .FailIf(user => !user.IsOverAge(18), UserErrors.UnderAge)
    .ThenDo(user => _logger.LogInformation($"User {user.Id} incremented age to {user.Age}"))
    .ThenAsync(user => _userRepository.UpdateAsync(user))
    .Match(
        _ => NoContent(),
        errors => errors.ToActionResult());
```

### Simple Example with intermediate steps

#### No Failure

```cs
ErrorOr<string> foo = await "2".ToErrorOr()
    .Then(int.Parse) // 2
    .FailIf(val => val > 2, Error.Validation(description: $"{val} is too big") // 2
    .ThenDoAsync(Task.Delay) // Sleep for 2 milliseconds
    .ThenDo(val => Console.WriteLine($"Finished waiting {val} milliseconds.")) // Finished waiting 2 milliseconds.
    .ThenAsync(val => Task.FromResult(val * 2)) // 4
    .Then(val => $"The result is {val}") // "The result is 4"
    .Else(errors => Error.Unexpected(description: "Yikes")) // "The result is 4"
    .MatchFirst(
        value => value, // "The result is 4"
        firstError => $"An error occurred: {firstError.Description}");
```

#### Failure

```cs
ErrorOr<string> foo = await "5".ToErrorOr()
    .Then(int.Parse) // 5
    .FailIf(val => val > 2, Error.Validation(description: $"{val} is too big") // Error.Validation()
    .ThenDoAsync(Task.Delay) // Error.Validation()
    .ThenDo(val => Console.WriteLine($"Finished waiting {val} milliseconds.")) // Error.Validation()
    .ThenAsync(val => Task.FromResult(val * 2)) // Error.Validation()
    .Then(val => $"The result is {val}") // Error.Validation()
    .Else(errors => Error.Unexpected(description: "Yikes")) // Error.Unexpected()
    .MatchFirst(
        value => value,
        firstError => $"An error occurred: {firstError.Description}"); // An error occurred: Yikes
```


# Creating an `ErrorOr` instance

## Using implicit conversion

There are implicit converters from `TResult`, `Error`, `List<Error>` to `ErrorOr<TResult>`

```cs
ErrorOr<int> result = 5;
ErrorOr<int> result = Error.Unexpected();
ErrorOr<int> result = [Error.Validation(), Error.Validation()];
```

```cs
public ErrorOr<int> IntToErrorOr()
{
    return 5;
}
```

```cs
public ErrorOr<int> SingleErrorToErrorOr()
{
    return Error.Unexpected();
}
```

```cs
public ErrorOr<int> MultipleErrorsToErrorOr()
{
    return [
        Error.Validation(description: "Invalid Name"),
        Error.Validation(description: "Invalid Last Name")
    ];
}
```

## Using The `ErrorOrFactory`

```cs
ErrorOr<int> result = ErrorOrFactory.From(5);
ErrorOr<int> result = ErrorOrFactory.From<int>(Error.Unexpected());
ErrorOr<int> result = ErrorOrFactory.From<int>([Error.Validation(), Error.Validation()]);
```

```cs
public ErrorOr<int> GetValue()
{
    return ErrorOrFactory.From(5);
}
```

```cs
public ErrorOr<int> SingleErrorToErrorOr()
{
    return ErrorOrFactory.From<int>(Error.Unexpected());
}
```

```cs
public ErrorOr<int> MultipleErrorsToErrorOr()
{
    return ErrorOrFactory.From([
        Error.Validation(description: "Invalid Name"),
        Error.Validation(description: "Invalid Last Name")
    ]);
}
```

## Using The `ToErrorOr` Extension Method

```cs
ErrorOr<int> result = 5.ToErrorOr();
ErrorOr<int> result = Error.Unexpected().ToErrorOr<int>();
ErrorOr<int> result = new[] { Error.Validation(), Error.Validation() }.ToErrorOr<int>();
```

# Properties

## `IsError`

```cs
ErrorOr<int> result = User.Create();

if (result.IsError)
{
    // the result contains one or more errors
}
```

## `Value`

```cs
ErrorOr<int> result = User.Create();

if (!result.IsError) // the result contains a value
{
    Console.WriteLine(result.Value);
}
```

## `Errors`

```cs
ErrorOr<int> result = User.Create();

if (result.IsError)
{
    result.Errors // contains the list of errors that occurred
        .ForEach(error => Console.WriteLine(error.Description));
}
```

## `FirstError`

```cs
ErrorOr<int> result = User.Create();

if (result.IsError)
{
    var firstError = result.FirstError; // only the first error that occurred
    Console.WriteLine(firstError == result.Errors[0]); // true
}
```

## `ErrorsOrEmptyList`

```cs
ErrorOr<int> result = User.Create();

if (result.IsError)
{
    result.ErrorsOrEmptyList // List<Error> { /* one or more errors */  }
    return;
}

result.ErrorsOrEmptyList // List<Error> { }
```

## `TryGetValue`

Safely extract the value without risking an exception:

```cs
if (result.TryGetValue(out var user))
{
    Console.WriteLine($"Got user: {user.Name}");
}
else
{
    Console.WriteLine("Result was an error");
}
```

## `TryGetErrors`

Safely extract errors without risking an exception:

```cs
if (result.TryGetErrors(out var errors))
{
    foreach (var error in errors)
    {
        Console.WriteLine($"Error: {error.Code}");
    }
}
```

## `GetValueOrDefault`

Get the value or a fallback if in error state:

```cs
User user = result.GetValueOrDefault(User.Anonymous);
int count = result.GetValueOrDefault(); // returns default(int) = 0 if error
```

# Methods

## `Match`

The `Match` method receives two functions, `onValue` and `onError`, `onValue` will be invoked if the result is success, and `onError` is invoked if the result is an error.

### `Match`

```cs
string foo = result.Match(
    value => value,
    errors => $"{errors.Count} errors occurred.");
```

### `MatchAsync`

```cs
string foo = await result.MatchAsync(
    value => Task.FromResult(value),
    errors => Task.FromResult($"{errors.Count} errors occurred."));
```

### `MatchFirst`

The `MatchFirst` method receives two functions, `onValue` and `onError`, `onValue` will be invoked if the result is success, and `onError` is invoked if the result is an error.

Unlike `Match`, if the state is error, `MatchFirst`'s `onError` function receives only the first error that occurred, not the entire list of errors.


```cs
string foo = result.MatchFirst(
    value => value,
    firstError => firstError.Description);
```

### `MatchFirstAsync`

```cs
string foo = await result.MatchFirstAsync(
    value => Task.FromResult(value),
    firstError => Task.FromResult(firstError.Description));
```

## `Switch`

The `Switch` method receives two actions, `onValue` and `onError`, `onValue` will be invoked if the result is success, and `onError` is invoked if the result is an error.

### `Switch`

```cs
result.Switch(
    value => Console.WriteLine(value),
    errors => Console.WriteLine($"{errors.Count} errors occurred."));
```

### `SwitchAsync`

```cs
await result.SwitchAsync(
    value => { Console.WriteLine(value); return Task.CompletedTask; },
    errors => { Console.WriteLine($"{errors.Count} errors occurred."); return Task.CompletedTask; });
```

### `SwitchFirst`

The `SwitchFirst` method receives two actions, `onValue` and `onError`, `onValue` will be invoked if the result is success, and `onError` is invoked if the result is an error.

Unlike `Switch`, if the state is error, `SwitchFirst`'s `onError` function receives only the first error that occurred, not the entire list of errors.

```cs
result.SwitchFirst(
    value => Console.WriteLine(value),
    firstError => Console.WriteLine(firstError.Description));
```

###  `SwitchFirstAsync`

```cs
await result.SwitchFirstAsync(
    value => { Console.WriteLine(value); return Task.CompletedTask; },
    firstError => { Console.WriteLine(firstError.Description); return Task.CompletedTask; });
```

## `Then`

### `Then`

`Then` receives a function, and invokes it only if the result is not an error.

```cs
ErrorOr<int> foo = result
    .Then(val => val * 2);
```

Multiple `Then` methods can be chained together.

```cs
ErrorOr<string> foo = result
    .Then(val => val * 2)
    .Then(val => $"The result is {val}");
```

If any of the methods return an error, the chain will break and the errors will be returned.

```cs
ErrorOr<int> Foo() => Error.Unexpected();

ErrorOr<string> foo = result
    .Then(val => val * 2)
    .Then(_ => GetAnError())
    .Then(val => $"The result is {val}") // this function will not be invoked
    .Then(val => $"The result is {val}"); // this function will not be invoked
```

### `ThenAsync`

`ThenAsync` receives an asynchronous function, and invokes it only if the result is not an error.

```cs
ErrorOr<string> foo = await result
    .ThenAsync(val => DoSomethingAsync(val))
    .ThenAsync(val => DoSomethingElseAsync($"The result is {val}"));
```

### `ThenDo` and `ThenDoAsync`

`ThenDo` and `ThenDoAsync` are similar to `Then` and `ThenAsync`, but instead of invoking a function that returns a value, they invoke an action.

```cs
ErrorOr<string> foo = result
    .ThenDo(val => Console.WriteLine(val))
    .ThenDo(val => Console.WriteLine($"The result is {val}"));
```

```cs
ErrorOr<string> foo = await result
    .ThenDoAsync(val => Task.Delay(val))
    .ThenDo(val => Console.WriteLine($"Finsihed waiting {val} seconds."))
    .ThenDoAsync(val => Task.FromResult(val * 2))
    .ThenDo(val => $"The result is {val}");
```

### Mixing `Then`, `ThenDo`, `ThenAsync`, `ThenDoAsync`

You can mix and match `Then`, `ThenDo`, `ThenAsync`, `ThenDoAsync` methods.

```cs
ErrorOr<string> foo = await result
    .ThenDoAsync(val => Task.Delay(val))
    .Then(val => val * 2)
    .ThenAsync(val => DoSomethingAsync(val))
    .ThenDo(val => Console.WriteLine($"Finsihed waiting {val} seconds."))
    .ThenAsync(val => Task.FromResult(val * 2))
    .Then(val => $"The result is {val}");
```

## `FailIf`

`FailIf` receives a predicate and an error. If the predicate is true, `FailIf` will return the error. Otherwise, it will return the value of the result.

```cs
ErrorOr<int> foo = result
    .FailIf(val => val > 2, Error.Validation(description: $"{val} is too big"));
```

Once an error is returned, the chain will break and the error will be returned.

```cs
var result = "2".ToErrorOr()
    .Then(int.Parse) // 2
    .FailIf(val => val > 1, Error.Validation(description: $"{val} is too big") // validation error
    .Then(num => num * 2) // this function will not be invoked
    .Then(num => num * 2) // this function will not be invoked
```

## `Else`

`Else` receives a value or a function. If the result is an error, `Else` will return the value or invoke the function. Otherwise, it will return the value of the result.

### `Else`

```cs
ErrorOr<string> foo = result
    .Else("fallback value");
```

```cs
ErrorOr<string> foo = result
    .Else(errors => $"{errors.Count} errors occurred.");
```

### `ElseAsync`

```cs
ErrorOr<string> foo = await result
    .ElseAsync(Task.FromResult("fallback value"));
```

```cs
ErrorOr<string> foo = await result
    .ElseAsync(errors => Task.FromResult($"{errors.Count} errors occurred."));
```

# Mixing Features (`Then`, `FailIf`, `Else`, `Switch`, `Match`)

You can mix `Then`, `FailIf`, `Else`, `Switch` and `Match` methods together.

```cs
ErrorOr<string> foo = await result
    .ThenDoAsync(val => Task.Delay(val))
    .FailIf(val => val > 2, Error.Validation(description: $"{val} is too big"))
    .ThenDo(val => Console.WriteLine($"Finished waiting {val} seconds."))
    .ThenAsync(val => Task.FromResult(val * 2))
    .Then(val => $"The result is {val}")
    .Else(errors => Error.Unexpected())
    .MatchFirst(
        value => value,
        firstError => $"An error occurred: {firstError.Description}");
```

# Error Types

Each `Error` instance has a `Type` property, which is an enum value that represents the type of the error.

## Built in error types

The following error types are built in:

```cs
public enum ErrorType
{
    Failure,
    Unexpected,
    Validation,
    Conflict,
    NotFound,
    Unauthorized,
    Forbidden,
    BadRequest,
    Timeout,
    Gone,
    Locked,
    TooManyRequests,
    Unavailable,
}
```

Each error type has a static method that creates an error of that type. For example:

```cs
var error = Error.NotFound();
```

optionally, you can pass a code, description and metadata to the error:

```cs
var error = Error.Unexpected(
    code: "User.ShouldNeverHappen",
    description: "A user error that should never happen",
    metadata: new Dictionary<string, object>
    {
        { "user", user },
    });
```
The `ErrorType` enum is a good way to categorize errors.

## Custom error types

You can create your own error types if you would like to categorize your errors differently.

A custom error type can be created with the `Custom` static method

```cs
public static class MyErrorTypes
{
    const int ShouldNeverHappen = 12;
}

var error = Error.Custom(
    type: MyErrorTypes.ShouldNeverHappen,
    code: "User.ShouldNeverHappen",
    description: "A user error that should never happen");
```

You can use the `Error.NumericType` method to retrieve the numeric type of the error.

```cs
var errorMessage = Error.NumericType switch
{
    MyErrorType.ShouldNeverHappen => "Consider replacing dev team",
    _ => "An unknown error occurred.",
};
```

# Built in result types (`Result.Success`, ..)

There are a few built in result types:

```cs
ErrorOr<Success> result = Result.Success;
ErrorOr<Created> result = Result.Created;
ErrorOr<Updated> result = Result.Updated;
ErrorOr<Deleted> result = Result.Deleted;
```

Which can be used as following

```cs
ErrorOr<Deleted> DeleteUser(Guid id)
{
    var user = await _userRepository.GetByIdAsync(id);
    if (user is null)
    {
        return Error.NotFound(description: "User not found.");
    }

    await _userRepository.DeleteAsync(user);
    return Result.Deleted;
}
```

# Additional Methods

## `MapError`

`MapError` transforms errors without affecting the value path. Useful for error enrichment or translation.

```cs
result.MapError(error => Error.Validation(
    code: $"API.{error.Code}",
    description: error.Description,
    metadata: new Dictionary<string, object> { { "OriginalType", error.Type } }));
```

### `MapErrors`

Transform the entire error list:

```cs
result.MapErrors(errors => errors
    .Where(e => e.Type == ErrorType.Validation)
    .ToList());
```

## `OrElse`

Unlike `Else` which always recovers to a value, `OrElse` chains recovery attempts that might also fail.

```cs
// Try primary, then fallback to cache, then to default
primarySource.GetUser(id)
    .OrElse(_ => cache.GetUser(id))
    .OrElse(_ => User.CreateDefault());
```

## `GetValueOrThrow`

Safely extract the value or throw with a descriptive error message:

```cs
var user = result.GetValueOrThrow(); // Throws with error codes if failed
var user = result.GetValueOrThrow("User must be present for this operation");
```

## `Flatten`

Handle nested `ErrorOr<ErrorOr<T>>` scenarios:

```cs
ErrorOr<ErrorOr<User>> nested = GetNestedResult();
ErrorOr<User> flattened = nested.Flatten();
```

## Tuple Deconstruction

Deconstruct ErrorOr into its components:

```cs
var (value, errors) = result;
if (errors is not null)
{
    // handle errors
}
else
{
    // use value
}

// Or with three parameters
var (isError, value, errors) = result;
```

## `ErrorOrUnorderedEqualityComparer`

Compare ErrorOr instances without considering error order:

```cs
var comparer = ErrorOrUnorderedEqualityComparer<int>.Instance;
var areEqual = comparer.Equals(result1, result2); // true if same errors in any order
```

## `Try` and `TryAsync`

`Try` executes a function and wraps any thrown exception as an `Error`, allowing safe interop with exception-throwing code.

```cs
// Basic usage - wraps exceptions as Unexpected errors
ErrorOr<int> result = ErrorOr<int>.Try(() => int.Parse("not a number"));
// result.FirstError.Code == "FormatException"
// result.FirstError.Description == "The input string 'not a number' was not in a correct format."
```

```cs
// With custom error mapping
ErrorOr<User> result = ErrorOr<User>.Try(
    () => repository.GetUser(id),
    ex => Error.Failure("Database.Error", ex.Message));
```

### `TryAsync`

```cs
ErrorOr<Data> result = await ErrorOr<Data>.TryAsync(
    async () => await httpClient.GetFromJsonAsync<Data>(url));
```

```cs
// With async error mapping
ErrorOr<Data> result = await ErrorOr<Data>.TryAsync(
    async () => await httpClient.GetFromJsonAsync<Data>(url),
    async ex => await LogAndCreateError(ex));
```

## `Combine` and `Collect`

`Combine` aggregates multiple `ErrorOr` results, returning a tuple of all values if successful, or all accumulated errors if any failed.

```cs
var nameResult = ValidateName(name);
var emailResult = ValidateEmail(email);
var ageResult = ValidateAge(age);

// Combine up to 8 results (returns tuple on success)
var combined = ErrorOrCombine.Combine(nameResult, emailResult, ageResult);
combined.Match(
    tuple => CreateUser(tuple.First, tuple.Second, tuple.Third),
    errors => HandleAllErrors(errors)); // All errors from all failed validations
```

### `Collect`

`Collect` aggregates a sequence of `ErrorOr<T>` results into a single `ErrorOr<List<T>>`:

```cs
var userIds = new[] { 1, 2, 3, 4, 5 };
var userResults = userIds.Select(id => GetUser(id)); // IEnumerable<ErrorOr<User>>

ErrorOr<List<User>> allUsers = ErrorOrCombine.Collect(userResults);
// Returns List<User> if all succeeded, or all accumulated errors
```

## LINQ-Style Methods

### `Select` and `SelectAsync`

`Select` is an alias for `Then`, providing LINQ query syntax compatibility:

```cs
ErrorOr<string> result = errorOr.Select(value => value.ToString());

// Async variant
ErrorOr<Data> result = await errorOr.SelectAsync(async value => await TransformAsync(value));
```

### `SelectMany` and `SelectManyAsync`

`SelectMany` chains operations that return `ErrorOr`, enabling LINQ query syntax:

```cs
ErrorOr<Order> result = errorOr.SelectMany(user => GetOrderForUser(user.Id));

// Can be used with LINQ query syntax
var result =
    from user in GetUser(userId)
    from order in GetOrder(user.DefaultOrderId)
    from item in GetFirstItem(order.Id)
    select item;
```

### `Where` and `WhereAsync`

`Where` filters values based on a predicate, returning an error if the predicate fails:

```cs
ErrorOr<int> result = errorOr
    .Where(value => value > 0, Error.Validation("Value.NonPositive", "Value must be positive"));

// With error factory (access to the value)
ErrorOr<User> result = errorOr
    .Where(
        user => user.IsActive,
        user => Error.Validation("User.Inactive", $"User {user.Id} is not active"));

// Async predicate
ErrorOr<User> result = await errorOr
    .WhereAsync(
        async user => await IsUserAuthorizedAsync(user),
        Error.Unauthorized());
```

# Organizing Errors

A nice approach, is creating a static class with the expected errors. For example:

```cs
public static partial class DivisionErrors
{
    public static Error CannotDivideByZero = Error.Unexpected(
        code: "Division.CannotDivideByZero",
        description: "Cannot divide by zero.");
}
```

Which can later be used as following 👇

```cs
public ErrorOr<float> Divide(int a, int b)
{
    if (b == 0)
    {
        return DivisionErrors.CannotDivideByZero;
    }

    return a / b;
}
```

# Feature Pipeline (VSA)

VsaResults includes a feature pipeline designed for Vertical Slice Architecture. Features encapsulate a complete vertical slice of functionality with built-in validation, execution, and observability.

## Feature Types

| Interface | Pipeline Stages | Use Case |
|-----------|-----------------|----------|
| `IQueryFeature<TRequest, TResult>` | Validate → Execute Query | Read-only operations |
| `IMutationFeature<TRequest, TResult>` | Validate → Enforce Requirements → Execute Mutation → Run Side Effects | State-changing operations |

## Query Feature Example

```cs
public static class GetUserById
{
    public record Request(Guid Id);

    public class Feature(
        IFeatureValidator<Request> validator,
        IFeatureQuery<Request, User> query)
        : IQueryFeature<Request, User>
    {
        public IFeatureValidator<Request> Validator => validator;
        public IFeatureQuery<Request, User> Query => query;
    }

    public class Validator : IFeatureValidator<Request>
    {
        public Task<ErrorOr<Request>> ValidateAsync(Request request, CancellationToken ct = default) =>
            request.Id == Guid.Empty
                ? Task.FromResult<ErrorOr<Request>>(Error.Validation("User.InvalidId", "User ID cannot be empty."))
                : Task.FromResult<ErrorOr<Request>>(request);
    }

    public class Query(IUserRepository repository) : IFeatureQuery<Request, User>
    {
        public Task<ErrorOr<User>> ExecuteAsync(Request request, CancellationToken ct = default) =>
            Task.FromResult(repository.GetById(request.Id));
    }
}

// Execute the feature
var feature = new GetUserById.Feature(new GetUserById.Validator(), new GetUserById.Query(repo));
var result = await feature.ExecuteAsync(new GetUserById.Request(userId));
```

## Mutation Feature Example

```cs
public static class CreateUser
{
    public record Request(string Email, string Name);

    public class Feature(
        IFeatureValidator<Request> validator,
        IFeatureMutator<Request, User> mutator,
        IFeatureSideEffects<Request>? sideEffects = null)
        : IMutationFeature<Request, User>
    {
        public IFeatureValidator<Request> Validator => validator;
        public IFeatureMutator<Request, User> Mutator => mutator;
        public IFeatureSideEffects<Request> SideEffects => sideEffects ?? NoOpSideEffects<Request>.Instance;
    }

    public class Validator : IFeatureValidator<Request>
    {
        public Task<ErrorOr<Request>> ValidateAsync(Request request, CancellationToken ct = default)
        {
            var errors = new List<Error>();
            if (string.IsNullOrWhiteSpace(request.Email))
                errors.Add(Error.Validation("User.InvalidEmail", "Email is required."));
            if (string.IsNullOrWhiteSpace(request.Name))
                errors.Add(Error.Validation("User.InvalidName", "Name is required."));

            return errors.Count > 0
                ? Task.FromResult<ErrorOr<Request>>(errors)
                : Task.FromResult<ErrorOr<Request>>(request);
        }
    }

    public class Mutator(IUserRepository repository) : IFeatureMutator<Request, User>
    {
        public async Task<ErrorOr<User>> ExecuteAsync(FeatureContext<Request> context, CancellationToken ct = default)
        {
            var request = context.Request;

            if (repository.ExistsByEmail(request.Email))
                return Error.Conflict("User.DuplicateEmail", $"Email {request.Email} is already registered.");

            var user = new User(Guid.NewGuid(), request.Email, request.Name, DateTime.UtcNow);
            repository.Add(user);

            // Add context for wide event logging
            context.AddContext("user_id", user.Id);

            return user;
        }
    }
}
```

## Pipeline Components

| Component | Interface | Purpose |
|-----------|-----------|---------|
| **Validator** | `IFeatureValidator<TRequest>` | Validates the incoming request |
| **Requirements** | `IFeatureRequirements<TRequest>` | Loads entities and enforces business rules (mutations only) |
| **Mutator** | `IFeatureMutator<TRequest, TResult>` | Executes the core mutation logic |
| **Query** | `IFeatureQuery<TRequest, TResult>` | Executes read-only queries |
| **Side Effects** | `IFeatureSideEffects<TRequest>` | Runs post-success effects like notifications (mutations only) |

Each component has a no-op default (`NoOpValidator`, `NoOpRequirements`, `NoOpSideEffects`) so you only implement what you need.

# Wide Events

Wide Events (also known as Canonical Log Lines) capture a single comprehensive structured log entry per feature execution. Instead of scattered log lines, you get one event with full context for debugging and observability.

## Key Principles

- **One event per execution** - Not scattered log lines throughout the code
- **High cardinality fields** - `user_id`, `trace_id`, `feature_name` for precise filtering
- **High dimensionality** - Many fields for rich querying in your observability stack
- **Build throughout, emit once** - Context accumulates during execution, emitted at the end

## Wide Event Fields

The `FeatureWideEvent` includes:

| Category | Fields |
|----------|--------|
| **Trace Context** | `TraceId`, `SpanId`, `ParentSpanId` |
| **Feature Context** | `FeatureName`, `FeatureType`, `RequestType`, `ResultType` |
| **Service Context** | `ServiceName`, `ServiceVersion`, `Environment`, `Region`, `Host` |
| **Pipeline Metadata** | `ValidatorType`, `RequirementsType`, `MutatorType`, `SideEffectsType` |
| **Timing** | `ValidationMs`, `RequirementsMs`, `ExecutionMs`, `SideEffectsMs`, `TotalMs` |
| **Outcome** | `Outcome` (success, validation_failure, execution_failure, etc.) |
| **Error Context** | `ErrorCode`, `ErrorType`, `ErrorMessage`, `FailedAtStage` |
| **Business Context** | `RequestContext` dictionary with custom fields |

## Using Wide Events

```cs
// Create an emitter (uses ILogger under the hood)
var emitter = new SerilogWideEventEmitter(logger);

// Execute with wide event emission
var result = await feature.ExecuteAsync(request, emitter);
```

## Custom Emitters

Implement `IWideEventEmitter` to integrate with your telemetry system:

```cs
public class OpenTelemetryWideEventEmitter : IWideEventEmitter
{
    public void Emit(FeatureWideEvent wideEvent)
    {
        // Emit to OpenTelemetry, Datadog, Honeycomb, etc.
    }
}
```

## Adding Business Context

Add custom fields to the wide event during execution:

```cs
public class Mutator : IFeatureMutator<Request, User>
{
    public async Task<ErrorOr<User>> ExecuteAsync(FeatureContext<Request> context, CancellationToken ct)
    {
        // ... create user ...

        // These fields appear in RequestContext
        context.AddContext("user_id", user.Id);
        context.AddContext("user_role", user.Role.ToString());
        context.AddContext("welcome_email_sent", true);

        return user;
    }
}
```

## Example Wide Event Output

```json
{
  "FeatureName": "CreateUser",
  "FeatureType": "Mutation",
  "Outcome": "success",
  "TotalMs": 45.23,
  "ValidationMs": 1.2,
  "RequirementsMs": 0,
  "ExecutionMs": 43.8,
  "SideEffectsMs": 0.23,
  "TraceId": "abc123",
  "RequestContext": {
    "user_id": "550e8400-e29b-41d4-a716-446655440000",
    "user_role": "Admin"
  }
}
```

# Samples

A complete sample Web API is available in `samples/VsaResults.Sample.WebApi` demonstrating:

- **Query Features**: `GetUserById`, `GetAllUsers`, `GetAllProducts`
- **Mutation Features**: `CreateUser`, `DeleteUser`, `ReserveStock`
- **ASP.NET Core Integration**: Minimal APIs and MVC controllers
- **Wide Event Emission**: Serilog integration
- **Messaging**: MassTransit consumers with message-wide events

Run the sample:

```bash
cd samples/VsaResults.Sample.WebApi
dotnet run
```

# API Reference

The NuGet packages include XML documentation. Generate a browsable reference site with tools like DocFX if you need hosted API docs.

# Contribution 🤲

If you have any questions, comments, or suggestions, please open an issue or create a pull request 🙂

# Credits 🙏

- [Amichai Mantinband](https://github.com/amantinband) - Creator of the original [ErrorOr](https://github.com/amantinband/error-or) library that this project is based on
- [OneOf](https://github.com/mcintyre321/OneOf/tree/master/OneOf) - An awesome library which provides F# style discriminated unions behavior for C#

# License 🪪

This project is licensed under the terms of the [MIT](https://github.com/mantinband/error-or/blob/main/LICENSE) license.

