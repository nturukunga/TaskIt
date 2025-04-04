# TaskIt - Distributed Task Management System

## Installation and Setup Guide

This document provides a comprehensive guide to setting up and running the TaskIt distributed task management system locally on a Windows machine.

## Prerequisites

Before you begin, ensure you have the following installed:

1. **Visual Studio 2022** - [Download](https://visualstudio.microsoft.com/vs/)
   - During installation, select the "ASP.NET and web development" workload
   - Ensure that ".NET 7.0" is included in the installation

2. **PostgreSQL** - [Download](https://www.postgresql.org/download/windows/)
   - Recommended version: PostgreSQL 14 or newer
   - Note the password you set during installation
   - Default port is 5432

3. **Git** (optional, for cloning the repository) - [Download](https://git-scm.com/download/win)

## Database Setup

1. **Create PostgreSQL Database**
   
   After installing PostgreSQL, you can create a database using pgAdmin (GUI) or psql (command line):

   **Using pgAdmin:**
   - Open pgAdmin from the Start menu
   - Connect to your PostgreSQL server
   - Right-click on "Databases" and select "Create" > "Database"
   - Name the database "TaskIt" and click "Save"

   **Using psql:**
   - Open Command Prompt
   - Connect to PostgreSQL with the following command:
     ```
     psql -U postgres
     ```
   - Enter your PostgreSQL password when prompted
   - Create the database:
     ```
     CREATE DATABASE "TaskIt";
     