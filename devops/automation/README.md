Continuous Integration
======================

Binding Tools for Swift is built on [internal Jenkins][1]. Jenkins will build all pull
requests (both from forks and the xamarin repository), and all branches on the
xamarin repository (but branches from forks).

The Jenkins job is a multibranch pipeline job described by the Jenkinsfile
file in this directory.

Jenkins steps
=============

Precheck
--------

Checks if any Binding Tools for Swift files were modified; if not don't do anything
else. This is necessary because this repository contains other code as well -
quite a few commits are completely unrelated to Binding Tools for Swift.

Provision
---------

Provisions the bot using our system-dependencies.sh script. What to provision is
described in the Make.config file.

Build Swift
-----------

If needed, build swift and upload a package of it, otherwise download a
previously uploaded swift package.

Build
-----

Build Binding Tools for Swift.

Package
-------

Create a Binding Tools for Swift package.

Sign
----

Sign the package we just created.

Upload to Azure
---------------

Upload the package we just created to Azure.

Publish builds to GitHub
------------------------

Create a GitHub status that points to the package we just uploaded to Azure.

Run tests
---------

Run tests 😉

Cleanup
-------

Binding Tools for Swift requires a significant amount of hard disk space, so do some
final cleanup to make sure we don't leave unnecessary stuff behind.

[1]: https://jenkins.internalx.com/blue/organizations/jenkins/swift-o-matic/activity
