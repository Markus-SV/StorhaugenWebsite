-- ============================================
-- Storhaugen Eats - Multi-Tenant Database Schema
-- Database: PostgreSQL (Supabase)
-- ============================================

-- Enable UUID extension
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- ============================================
-- TABLE: households
-- Represents a group/family sharing a meal list
-- ============================================
CREATE TABLE households (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(255) NOT NULL,
    leader_id UUID, -- Foreign key to users (set after users table created)
    settings JSONB DEFAULT '{}', -- Theme, preferences, etc.
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- ============================================
-- TABLE: users
-- Individual user accounts (integrated with Supabase Auth)
-- ============================================
CREATE TABLE users (
    id UUID PRIMARY KEY REFERENCES auth.users(id) ON DELETE CASCADE, -- Links to Supabase Auth
    email VARCHAR(255) UNIQUE NOT NULL,
    display_name VARCHAR(255) NOT NULL,
    avatar_url TEXT,
    unique_share_id VARCHAR(12) UNIQUE NOT NULL, -- For household invites (e.g., "ABC123XYZ")
    current_household_id UUID REFERENCES households(id) ON DELETE SET NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Add foreign key constraint to households.leader_id now that users exists
ALTER TABLE households ADD CONSTRAINT fk_household_leader
    FOREIGN KEY (leader_id) REFERENCES users(id) ON DELETE SET NULL;

-- ============================================
-- TABLE: global_recipes
-- Source of truth for all HelloFresh items and public user-created recipes
-- ============================================
CREATE TABLE global_recipes (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    title VARCHAR(255) NOT NULL,
    description TEXT,
    image_url TEXT, -- Hosted on Supabase Storage
    ingredients JSONB NOT NULL DEFAULT '[]', -- [{ name, amount, unit, image }]
    nutrition_data JSONB, -- { calories, protein, carbs, fat, etc. }
    cook_time_minutes INTEGER,
    difficulty VARCHAR(50), -- "Easy", "Medium", "Hard"

    -- Source tracking
    is_hellofresh BOOLEAN DEFAULT FALSE,
    hellofresh_uuid VARCHAR(255) UNIQUE, -- Original HelloFresh ID
    hellofresh_slug VARCHAR(255), -- URL slug from HelloFresh
    hellofresh_week VARCHAR(20), -- Week the recipe was available (e.g., "2026-W02")
    created_by_user_id UUID REFERENCES users(id) ON DELETE SET NULL, -- NULL if HelloFresh

    -- Additional metadata
    tags JSONB DEFAULT '[]', -- Tags like "Barnevennlig", "Rask", etc.
    cuisine VARCHAR(100), -- e.g., "Fusion", "Asiatiske"
    servings INTEGER,
    prep_time_minutes INTEGER,
    total_time_minutes INTEGER,

    -- Visibility
    is_public BOOLEAN DEFAULT FALSE, -- Only relevant for user-created items

    -- Aggregated ratings (denormalized for performance)
    average_rating DECIMAL(3,2) DEFAULT 0.00, -- Calculated from ratings table
    rating_count INTEGER DEFAULT 0,

    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Indexes for common queries
CREATE INDEX idx_global_recipes_hellofresh ON global_recipes(is_hellofresh) WHERE is_hellofresh = TRUE;
CREATE INDEX idx_global_recipes_public ON global_recipes(is_public) WHERE is_public = TRUE;
CREATE INDEX idx_global_recipes_rating ON global_recipes(average_rating DESC);
CREATE INDEX idx_global_recipes_hellofresh_uuid ON global_recipes(hellofresh_uuid);
CREATE INDEX idx_global_recipes_hellofresh_week ON global_recipes(hellofresh_week) WHERE hellofresh_week IS NOT NULL;

-- ============================================
-- TABLE: household_recipes
-- Local instances of recipes in a household's list
-- Implements "Reference vs Fork" logic via global_recipe_id
-- ============================================
CREATE TABLE household_recipes (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    household_id UUID NOT NULL REFERENCES households(id) ON DELETE CASCADE,

    -- Reference/Fork Logic
    global_recipe_id UUID REFERENCES global_recipes(id) ON DELETE SET NULL,
    -- If global_recipe_id IS NOT NULL → "Linked Mode" (display global data + personal notes)
    -- If global_recipe_id IS NULL → "Forked Mode" (completely local data)

    -- Local data (used when forked, or as personal notes when linked)
    local_title VARCHAR(255), -- Only used if forked
    local_description TEXT,
    local_ingredients JSONB, -- Only used if forked
    local_image_url TEXT, -- Only used if forked
    personal_notes TEXT, -- ALWAYS displayed (even in linked mode)

    -- Metadata
    added_by_user_id UUID REFERENCES users(id) ON DELETE SET NULL,
    is_archived BOOLEAN DEFAULT FALSE,

    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),

    -- Ensure household can't have duplicate global recipes (unless forked)
    UNIQUE NULLS NOT DISTINCT (household_id, global_recipe_id)
);

-- Indexes
CREATE INDEX idx_household_recipes_household ON household_recipes(household_id);
CREATE INDEX idx_household_recipes_global ON household_recipes(global_recipe_id);
CREATE INDEX idx_household_recipes_archived ON household_recipes(is_archived);

-- ============================================
-- TABLE: ratings
-- User ratings for global recipes
-- ============================================
CREATE TABLE ratings (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    global_recipe_id UUID NOT NULL REFERENCES global_recipes(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    score INTEGER NOT NULL CHECK (score >= 0 AND score <= 10),
    comment TEXT,

    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),

    -- One rating per user per recipe
    UNIQUE(global_recipe_id, user_id)
);

-- Indexes
CREATE INDEX idx_ratings_global_recipe ON ratings(global_recipe_id);
CREATE INDEX idx_ratings_user ON ratings(user_id);

-- ============================================
-- TABLE: household_invites
-- Pending invitations to join households
-- ============================================
CREATE TABLE household_invites (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    household_id UUID NOT NULL REFERENCES households(id) ON DELETE CASCADE,
    invited_user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    invited_by_user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    status VARCHAR(20) DEFAULT 'pending', -- 'pending', 'accepted', 'rejected'

    -- For merge requests (when invited user already has a household)
    merge_requested BOOLEAN DEFAULT FALSE,

    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),

    UNIQUE(household_id, invited_user_id)
);

-- ============================================
-- TABLE: etl_sync_log
-- Tracks HelloFresh ETL scraper runs
-- ============================================
CREATE TABLE etl_sync_log (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    sync_type VARCHAR(50) DEFAULT 'hellofresh', -- Future: other sources
    status VARCHAR(20), -- 'success', 'failed', 'partial'
    recipes_added INTEGER DEFAULT 0,
    recipes_updated INTEGER DEFAULT 0,
    error_message TEXT,
    build_id VARCHAR(255), -- HelloFresh build ID used
    weeks_synced VARCHAR(255), -- e.g., "2025-W51,2025-W52"

    started_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    completed_at TIMESTAMP WITH TIME ZONE
);

-- Index for checking last sync
CREATE INDEX idx_etl_sync_log_started ON etl_sync_log(started_at DESC);

-- ============================================
-- FUNCTIONS & TRIGGERS
-- ============================================

-- Function: Update updated_at timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Apply trigger to all tables with updated_at
CREATE TRIGGER update_households_updated_at BEFORE UPDATE ON households
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_users_updated_at BEFORE UPDATE ON users
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_global_recipes_updated_at BEFORE UPDATE ON global_recipes
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_household_recipes_updated_at BEFORE UPDATE ON household_recipes
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_ratings_updated_at BEFORE UPDATE ON ratings
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- Function: Recalculate global recipe ratings
CREATE OR REPLACE FUNCTION recalculate_global_recipe_rating(recipe_id UUID)
RETURNS VOID AS $$
BEGIN
    UPDATE global_recipes
    SET
        average_rating = COALESCE((
            SELECT AVG(score)::DECIMAL(3,2)
            FROM ratings
            WHERE global_recipe_id = recipe_id
        ), 0.00),
        rating_count = (
            SELECT COUNT(*)
            FROM ratings
            WHERE global_recipe_id = recipe_id
        )
    WHERE id = recipe_id;
END;
$$ LANGUAGE plpgsql;

-- Trigger: Auto-update ratings when rating is inserted/updated/deleted
CREATE OR REPLACE FUNCTION update_recipe_rating_on_change()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'DELETE' THEN
        PERFORM recalculate_global_recipe_rating(OLD.global_recipe_id);
    ELSE
        PERFORM recalculate_global_recipe_rating(NEW.global_recipe_id);
    END IF;
    RETURN NULL;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER ratings_update_recipe_stats
AFTER INSERT OR UPDATE OR DELETE ON ratings
FOR EACH ROW EXECUTE FUNCTION update_recipe_rating_on_change();

-- ============================================
-- ROW LEVEL SECURITY (RLS) POLICIES
-- Enable RLS for multi-tenancy security
-- ============================================

-- Enable RLS on all tables
ALTER TABLE households ENABLE ROW LEVEL SECURITY;
ALTER TABLE users ENABLE ROW LEVEL SECURITY;
ALTER TABLE global_recipes ENABLE ROW LEVEL SECURITY;
ALTER TABLE household_recipes ENABLE ROW LEVEL SECURITY;
ALTER TABLE ratings ENABLE ROW LEVEL SECURITY;
ALTER TABLE household_invites ENABLE ROW LEVEL SECURITY;

-- Policy: Users can read their own data
CREATE POLICY users_select_own ON users
    FOR SELECT USING (auth.uid() = id);

-- Policy: Users can update their own data
CREATE POLICY users_update_own ON users
    FOR UPDATE USING (auth.uid() = id);

-- Policy: Users can read their household
CREATE POLICY households_select_member ON households
    FOR SELECT USING (
        id IN (SELECT current_household_id FROM users WHERE id = auth.uid())
    );

-- Policy: Household leaders can update household
CREATE POLICY households_update_leader ON households
    FOR UPDATE USING (leader_id = auth.uid());

-- Policy: Anyone can read public global recipes
CREATE POLICY global_recipes_select_public ON global_recipes
    FOR SELECT USING (is_hellofresh = TRUE OR is_public = TRUE);

-- Policy: Users can read their household's recipes
CREATE POLICY household_recipes_select_own ON household_recipes
    FOR SELECT USING (
        household_id IN (SELECT current_household_id FROM users WHERE id = auth.uid())
    );

-- Policy: Users can insert/update/delete recipes in their household
CREATE POLICY household_recipes_modify_own ON household_recipes
    FOR ALL USING (
        household_id IN (SELECT current_household_id FROM users WHERE id = auth.uid())
    );

-- Policy: Users can read all ratings for public recipes
CREATE POLICY ratings_select_all ON ratings
    FOR SELECT USING (TRUE);

-- Policy: Users can insert/update their own ratings
CREATE POLICY ratings_modify_own ON ratings
    FOR ALL USING (user_id = auth.uid());

-- Policy: Users can see invites to their household or invites they sent
CREATE POLICY invites_select_relevant ON household_invites
    FOR SELECT USING (
        invited_user_id = auth.uid() OR
        invited_by_user_id = auth.uid() OR
        household_id IN (SELECT current_household_id FROM users WHERE id = auth.uid())
    );

-- ============================================
-- INITIAL DATA & HELPER FUNCTIONS
-- ============================================

-- Function: Generate unique share ID (12 chars, alphanumeric)
CREATE OR REPLACE FUNCTION generate_unique_share_id()
RETURNS VARCHAR(12) AS $$
DECLARE
    chars VARCHAR := 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789'; -- Exclude confusing chars
    result VARCHAR := '';
    i INTEGER;
    random_index INTEGER;
BEGIN
    FOR i IN 1..12 LOOP
        random_index := floor(random() * length(chars) + 1)::INTEGER;
        result := result || substr(chars, random_index, 1);
    END LOOP;
    RETURN result;
END;
$$ LANGUAGE plpgsql;

-- Function: Create household and set user as leader
CREATE OR REPLACE FUNCTION create_household_for_user(
    user_id UUID,
    household_name VARCHAR(255)
)
RETURNS UUID AS $$
DECLARE
    new_household_id UUID;
BEGIN
    -- Create household
    INSERT INTO households (name, leader_id)
    VALUES (household_name, user_id)
    RETURNING id INTO new_household_id;

    -- Assign user to household
    UPDATE users
    SET current_household_id = new_household_id
    WHERE id = user_id;

    RETURN new_household_id;
END;
$$ LANGUAGE plpgsql;

-- ============================================
-- VIEWS FOR COMMON QUERIES
-- ============================================

-- View: Household recipes with global data joined
CREATE OR REPLACE VIEW household_recipes_full AS
SELECT
    hr.id,
    hr.household_id,
    hr.global_recipe_id,
    hr.personal_notes,
    hr.is_archived,
    hr.added_by_user_id,
    hr.created_at,
    hr.updated_at,

    -- Display logic: Use global data if linked, local data if forked
    COALESCE(hr.local_title, gr.title) AS title,
    COALESCE(hr.local_description, gr.description) AS description,
    COALESCE(hr.local_image_url, gr.image_url) AS image_url,
    COALESCE(hr.local_ingredients, gr.ingredients) AS ingredients,

    -- Global recipe metadata (NULL if forked)
    gr.is_hellofresh,
    gr.average_rating,
    gr.rating_count,
    gr.nutrition_data,
    gr.cook_time_minutes,
    gr.difficulty,

    -- Linked/Forked indicator
    CASE
        WHEN hr.global_recipe_id IS NOT NULL THEN 'linked'
        ELSE 'forked'
    END AS recipe_mode

FROM household_recipes hr
LEFT JOIN global_recipes gr ON hr.global_recipe_id = gr.id;

COMMENT ON VIEW household_recipes_full IS 'Combines household recipes with global recipe data, handling linked vs forked logic';

-- ============================================
-- INDEXES FOR PERFORMANCE
-- ============================================

-- Additional indexes for joins
CREATE INDEX idx_users_household ON users(current_household_id);
CREATE INDEX idx_users_share_id ON users(unique_share_id);

-- ============================================
-- COMMENTS FOR DOCUMENTATION
-- ============================================

COMMENT ON TABLE households IS 'Groups/families sharing a meal list';
COMMENT ON TABLE users IS 'Individual user accounts linked to Supabase Auth';
COMMENT ON TABLE global_recipes IS 'Source of truth for HelloFresh and public recipes';
COMMENT ON TABLE household_recipes IS 'Local instances of recipes in household lists (linked or forked)';
COMMENT ON TABLE ratings IS 'User ratings for global recipes (1-10 scale)';
COMMENT ON TABLE household_invites IS 'Pending household invitations and merge requests';
COMMENT ON TABLE etl_sync_log IS 'Tracks HelloFresh scraper sync runs';

COMMENT ON COLUMN household_recipes.global_recipe_id IS 'NULL = forked/local recipe, NOT NULL = linked to global recipe';
COMMENT ON COLUMN household_recipes.personal_notes IS 'Always displayed, even for linked recipes';
