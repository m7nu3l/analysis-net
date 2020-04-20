# analysis-net
# Intro

**analysis-net** is a framework that focuses on static analysis of .NET programs.  It generates a [three-address code](https://en.wikipedia.org/wiki/Three-address_code) representation for the .NET bytecode ([CIL](https://en.wikipedia.org/wiki/Common_Intermediate_Language)) and its analyses are implemented on this type of instructions.  CIL is a stack-based bytecode while **analysis-net**'s code representation is register-based.

Aditionally, it has been extended to provide the ability to compile a .NET program from the three-address code representation. This feature unlocks the possiblity to implement instrumentations/optimizations on .NET programs but from a much nicer abstraction.

<p align="center">
<img src="/images/flow.svg">
</p>

# Available static analyses:

Control-flow and data-flow analysis framework for .NET programs.

+ Static analysis framework for .NET
    * Bytecode level
    * No need for source code
    * Can analyze standard libraries
    + Intermediate representation
        * Three address code
        * Static single assignment
    + Control-flow analysis
        * Dominance
        * Dominance frontier
        * Natural loops
    + Data-flow analysis
        * Def-Use and Use-Def chains
        * Copy propagation
        * Points-to analysis
    * Type inference
    * Web analysis
    + Serialization
        * DOT
        * DGML


# Status

|master| [![Build Status](https://travis-ci.com/m7nu3l/tac2cil.svg?token=f7qzBQCoptr4sx6YDGWa&branch=master)](https://travis-ci.com/m7nu3l/tac2cil) |
|--|--|
