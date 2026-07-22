# Architecture Overview

## 1. Product Shape

Librory will be delivered as a web application first.

Reasons:

- The user wants support for iPhone without requiring a MacBook-based native build workflow.
- A web app keeps the first release accessible on desktop and mobile browsers.
- Native app work can be revisited later if the product proves valuable.

## 2. Stack Decisions

### Frontend

Web frontend, with bilingual UI support.

Recommended stack:

- React
- Vite
- TypeScript
- Tailwind CSS
- Radix UI
- shadcn/ui-style component composition

Why this stack:

- It matches the existing Koviva frontend stack closely, which reduces context switching and lets us reuse patterns.
- It is well suited to a mobile-first web app that must work on iPhone without a native build pipeline.
- It keeps UI primitives composable while still allowing a custom visual identity.
- It fits well with a React Router based application and a lightweight API-driven architecture.

Recommended UI approach:

- Use shadcn/ui-style components as the default UI layer.
- Build a small Librory design system on top of Tailwind and Radix primitives.
- Keep the component set focused on book cards, scan panels, filters, language switching, and family-library views.

### Backend

ASP.NET Core API.

The API will own:

- Authentication and family membership
- Shelf scan session management
- Library ingestion
- Search and duplicate checks
- Wishlist operations
- Recommendation requests
- Localization-aware book data shaping

### AI Layer

Microsoft Agent Framework will be used for AI orchestration inside the main API project.

Primary responsibilities:

- Coordinate recognition, enrichment, and recommendation steps
- Call external tools and data sources
- Keep the AI logic separate from business rules
- Support workflow-style composition for scan and recommendation flows

This means the first version does not need a separate AI service project. The API hosts the AI orchestration layer directly, while still keeping the workflow code isolated in internal folders or modules.

### Azure Hosting

Azure will host the application and supporting services.

Recommended baseline:

- Azure App Service or Azure Container Apps for the web/API host
- Azure Database for PostgreSQL for relational data
- Azure Blob Storage for book images and scan artifacts
- Azure OpenAI or Azure AI services for model-backed features
- Azure AI Search later if search volume or metadata complexity grows

## 3. Functional Boundaries

### Web App

The web app should provide:

- Shelf scanning capture
- Search and browsing
- Book detail views
- Family library views
- Wishlist views
- Purchase intake views
- Bilingual UI switching

### API

The API should expose coarse-grained endpoints for:

- Scan sessions
- Book recognition results
- Book work / edition / copy records
- Family members
- Wishlist items
- Recommendation profiles
- Duplicate detection

### AI Workflow

The AI workflow should not own the database schema or UI state.

It should:

- Receive a scan request
- Produce candidate book matches
- Enrich the result with metadata
- Produce recommendation reasoning
- Produce duplicate warnings and edition differences
- Return structured output that the API can persist

## 4. Core Domain Split

### Work

The abstract literary work.

Examples:

- Book title
- Canonical author

### Edition

A specific publication or edition of a work.

Examples:

- Hardcover
- Paperback
- Illustrated edition
- Year-specific edition

### Copy

The physical copy owned by the family.

Examples:

- Purchase price
- Store
- Condition
- Shelf location
- Owning family member

This split is required for duplicate detection and edition-aware recommendations.

## 5. Bilingual Data Strategy

### UI Language

UI strings must be extracted into language resources.

Supported languages:

- English
- Chinese

### Book Data

Book records should store language-aware fields where needed:

- Title
- Subtitle
- Summary
- Genre labels
- Recommendation text
- Duplicate warning text

### Preferred Language

Each user or family profile should support a `preferredLanguage` field.

Purpose:

- Prefer Chinese display for users who are less comfortable reading English
- Keep English as the canonical data source where available
- Allow mixed-language display when both languages exist

## 6. AI Flow

### Shelf Scan Flow

1. User captures a shelf photo.
2. API creates a temporary scan session.
3. Agent Framework workflow extracts candidate titles from the image.
4. Metadata is resolved from book data sources.
5. Recommendation logic scores each candidate.
6. Duplicate detection checks the family library.
7. API returns a structured result list.

### Home Intake Flow

1. User rescans the purchased book.
2. System resolves the best edition match.
3. User confirms or corrects the result.
4. API persists the copy and purchase record.

### Recommendation Flow

1. User profile preferences are loaded.
2. AI produces a recommendation score and explanation.
3. Business rules apply constraints such as age fit and duplicate warnings.
4. The UI displays recommendation and duplicate results side by side.

## 7. Security and Identity

The first version should keep authentication simple.

Recommended baseline:

- Single-family or small-family accounts
- User login through Azure-backed identity or a simple auth provider
- Family membership stored in the app domain

## 8. Suggested Delivery Phases

### Phase 1

- Web app shell
- API project
- Database schema
- Localization resources
- Manual book intake

### Phase 2

- Shelf scan workflow
- Duplicate detection
- Recommendation profile
- Wishlist

### Phase 3

- AI enrichment
- Better metadata merging
- Smarter edition handling
- Expanded analytics

## 9. Open Decisions

The following should be finalized before implementation starts:

- Exact Azure hosting combination
- Book metadata providers
- Authentication provider
- Whether scan storage uses Blob Storage or database rows for temporary artifacts
