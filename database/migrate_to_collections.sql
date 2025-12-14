-- ============================================================
-- MIGRATION: Household-centric to Collection-centric model
-- This script drops all household tables and creates collection tables
-- NOTE: This is destructive - run only on test databases
-- ============================================================

BEGIN;

-- ============================================================
-- STEP 1: DROP RLS POLICIES REFERENCING HOUSEHOLDS
-- ============================================================

-- Drop policies on household tables (if they exist)
DROP POLICY IF EXISTS "Users can view households they belong to" ON households;
DROP POLICY IF EXISTS "Household leaders can update their household" ON households;
DROP POLICY IF EXISTS "Members can view household members" ON household_members;
DROP POLICY IF EXISTS "Leaders can manage household members" ON household_members;
DROP POLICY IF EXISTS "Members can view household recipes" ON household_recipes;
DROP POLICY IF EXISTS "Members can manage household recipes" ON household_recipes;
DROP POLICY IF EXISTS "Household invites are viewable by invitee" ON household_invites;
DROP POLICY IF EXISTS "Household friendships viewable by members" ON household_friendships;

-- Drop any policies on users table that reference current_household_id
DROP POLICY IF EXISTS "Users can view users in same household" ON users;

-- ============================================================
-- STEP 2: DROP FOREIGN KEY ON USERS TABLE
-- ============================================================

-- Drop the FK constraint on users.current_household_id
ALTER TABLE users DROP CONSTRAINT IF EXISTS fk_users_households_current_household_id;
ALTER TABLE users DROP CONSTRAINT IF EXISTS users_current_household_id_fkey;

-- Drop index on current_household_id
DROP INDEX IF EXISTS ix_users_current_household_id;

-- Drop the column
ALTER TABLE users DROP COLUMN IF EXISTS current_household_id;

-- ============================================================
-- STEP 3: DROP HOUSEHOLD-RELATED TABLES (CASCADE)
-- Order matters due to FK dependencies
-- ============================================================

DROP TABLE IF EXISTS household_friendships CASCADE;
DROP TABLE IF EXISTS household_invites CASCADE;
DROP TABLE IF EXISTS household_recipes CASCADE;
DROP TABLE IF EXISTS household_members CASCADE;
DROP TABLE IF EXISTS households CASCADE;

-- ============================================================
-- STEP 4: UPDATE user_recipes visibility values
-- Replace 'household' with 'private' (or 'friends')
-- ============================================================

UPDATE user_recipes SET visibility = 'private' WHERE visibility = 'household';

-- ============================================================
-- STEP 5: CREATE COLLECTIONS TABLE
-- ============================================================

CREATE TABLE IF NOT EXISTS collections (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL,
    owner_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Indexes
CREATE INDEX IF NOT EXISTS ix_collections_owner_id ON collections(owner_id);

-- ============================================================
-- STEP 6: CREATE COLLECTION_MEMBERS TABLE
-- ============================================================

CREATE TABLE IF NOT EXISTS collection_members (
    collection_id UUID NOT NULL REFERENCES collections(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    is_owner BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    PRIMARY KEY (collection_id, user_id)
);

-- Indexes
CREATE INDEX IF NOT EXISTS ix_collection_members_user_id ON collection_members(user_id);
CREATE INDEX IF NOT EXISTS ix_collection_members_collection_id ON collection_members(collection_id);

-- ============================================================
-- STEP 7: CREATE USER_RECIPE_COLLECTIONS TABLE
-- ============================================================

CREATE TABLE IF NOT EXISTS user_recipe_collections (
    user_recipe_id UUID NOT NULL REFERENCES user_recipes(id) ON DELETE CASCADE,
    collection_id UUID NOT NULL REFERENCES collections(id) ON DELETE CASCADE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    PRIMARY KEY (user_recipe_id, collection_id)
);

-- Indexes
CREATE INDEX IF NOT EXISTS ix_user_recipe_collections_collection_id ON user_recipe_collections(collection_id);
CREATE INDEX IF NOT EXISTS ix_user_recipe_collections_user_recipe_id ON user_recipe_collections(user_recipe_id);

-- ============================================================
-- STEP 8: RLS POLICIES FOR COLLECTIONS
-- ============================================================

-- Enable RLS
ALTER TABLE collections ENABLE ROW LEVEL SECURITY;
ALTER TABLE collection_members ENABLE ROW LEVEL SECURITY;
ALTER TABLE user_recipe_collections ENABLE ROW LEVEL SECURITY;

-- Collections policies
-- Members can SELECT collections they belong to
CREATE POLICY "Members can view collections they belong to"
ON collections FOR SELECT
USING (
    owner_id = auth.uid()
    OR EXISTS (
        SELECT 1 FROM collection_members cm
        WHERE cm.collection_id = id AND cm.user_id = auth.uid()
    )
);

-- Owners can INSERT collections (they own)
CREATE POLICY "Users can create collections"
ON collections FOR INSERT
WITH CHECK (owner_id = auth.uid());

-- Owners can UPDATE their collections
CREATE POLICY "Owners can update their collections"
ON collections FOR UPDATE
USING (owner_id = auth.uid())
WITH CHECK (owner_id = auth.uid());

-- Owners can DELETE their collections
CREATE POLICY "Owners can delete their collections"
ON collections FOR DELETE
USING (owner_id = auth.uid());

-- Collection members policies
-- Members can SELECT member rows for collections they belong to
CREATE POLICY "Members can view collection membership"
ON collection_members FOR SELECT
USING (
    user_id = auth.uid()
    OR EXISTS (
        SELECT 1 FROM collection_members cm
        WHERE cm.collection_id = collection_id AND cm.user_id = auth.uid()
    )
);

-- Owners can INSERT members
CREATE POLICY "Owners can add members"
ON collection_members FOR INSERT
WITH CHECK (
    EXISTS (
        SELECT 1 FROM collections c
        WHERE c.id = collection_id AND c.owner_id = auth.uid()
    )
    OR (user_id = auth.uid() AND is_owner = TRUE) -- Owner adding themselves
);

-- Owners can DELETE members
CREATE POLICY "Owners can remove members"
ON collection_members FOR DELETE
USING (
    EXISTS (
        SELECT 1 FROM collections c
        WHERE c.id = collection_id AND c.owner_id = auth.uid()
    )
    OR user_id = auth.uid() -- Members can leave
);

-- User recipe collections policies
-- Members can SELECT links for collections they belong to
CREATE POLICY "Members can view collection recipes"
ON user_recipe_collections FOR SELECT
USING (
    EXISTS (
        SELECT 1 FROM collection_members cm
        WHERE cm.collection_id = collection_id AND cm.user_id = auth.uid()
    )
);

-- Members can INSERT links (add recipes to collections they're members of)
CREATE POLICY "Members can add recipes to collections"
ON user_recipe_collections FOR INSERT
WITH CHECK (
    EXISTS (
        SELECT 1 FROM collection_members cm
        WHERE cm.collection_id = collection_id AND cm.user_id = auth.uid()
    )
);

-- Members can DELETE links (remove recipes from collections they're members of)
CREATE POLICY "Members can remove recipes from collections"
ON user_recipe_collections FOR DELETE
USING (
    EXISTS (
        SELECT 1 FROM collection_members cm
        WHERE cm.collection_id = collection_id AND cm.user_id = auth.uid()
    )
);

-- ============================================================
-- STEP 9: UPDATE TRIGGER FOR updated_at
-- ============================================================

-- Create or replace trigger function if it doesn't exist
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Add triggers
DROP TRIGGER IF EXISTS update_collections_updated_at ON collections;
CREATE TRIGGER update_collections_updated_at
    BEFORE UPDATE ON collections
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

DROP TRIGGER IF EXISTS update_collection_members_updated_at ON collection_members;
CREATE TRIGGER update_collection_members_updated_at
    BEFORE UPDATE ON collection_members
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

DROP TRIGGER IF EXISTS update_user_recipe_collections_updated_at ON user_recipe_collections;
CREATE TRIGGER update_user_recipe_collections_updated_at
    BEFORE UPDATE ON user_recipe_collections
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

COMMIT;

-- ============================================================
-- POST-MIGRATION VERIFICATION QUERIES (run manually)
-- ============================================================
-- SELECT COUNT(*) FROM collections;
-- SELECT COUNT(*) FROM collection_members;
-- SELECT COUNT(*) FROM user_recipe_collections;
-- SELECT COUNT(*) FROM user_recipes WHERE visibility = 'household'; -- Should be 0
