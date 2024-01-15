---
sidebar_position: 1
---

# Introduction

Welcome to Elmish Land, a framework designed to help you build reliable web apps. Heavily inspired by [Elm Land](https://elm.land).

## What is Elmish Land?

In the JavaScript ecosystem, the idea of an "application framework" is pretty common. In the React community, one popular framework is called Next.js. In the Vue.js community, you'll find a similar framework called Nuxt.

These frameworks help take care of the common questions you might encounter when getting started with a new project. They also include helpful guides and learning resources throughout your personal journey.

Elmish Land is no different! But instead of building apps in Javascript, you'll be using something different: F# and Elmish.

## The Elm Architecture

The Elm Architecture or TEA for short is an architecture for building modular user interfaces. It was popularized by the [Elm](https://elm-lang.org/) programming language which mainly uses the TEA programming model for building web applications.

Despite the name, this programming model is not restricted to the Elm language, and many other programming languages use a variant of this architecture that fits within the context of the language. For example, in the JavaScript world, you have [React](https://reactjs.org/) and [Redux](https://reactjs.org/) as one of the most popular TEA implementations. In F#, we have the [Elmish](https://elmish.github.io/elmish/) library: another implementation of TEA that fits very well with F# constructs.

Whatever application you might be building, there are almost always two main concerns that a UI application has to deal with:

* How to keep track of and maintain the state of the application
* How to keep the user interface in-sync with the state as it changes

The Elm Architecture provides a systematic approach for these problems using some building blocks. These blocks are divided into the following:

* `State`, also known as the `Model`, is a type that represents the data you want to keep track of while the application is running.
* `Messages` are the types of events that can change your state. Messages can either be triggered from the user interface or from external sources. The messages are modeled with a discriminated union.
* The `Update` function is a function which takes a triggered message or event, along with the current state, then calculates the next state of the application.
* The `Render` function, also known as the `View` function, takes the current state and builds the user interface from it. The user interface can trigger messages or events.

## Standing on the shoulders of giants

Much of this documentation is copied or ported from other great projects. This documentation would not have been possible without them!

* [The Elm Land documentation](https://elm.land/guide/)
* [The Elmish documentation](https://elmish.github.io/elmish/docs/basics.html)
* [The Elmish Book](https://zaid-ajaj.github.io/the-elmish-book/)
