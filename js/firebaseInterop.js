// Firebase Interop for Storhaugen Food Rating App
import { initializeApp } from "https://www.gstatic.com/firebasejs/10.8.0/firebase-app.js";
import {
    getFirestore,
    collection,
    addDoc,
    getDocs,
    doc,
    getDoc,
    updateDoc,
    query,
    where,
    orderBy
} from "https://www.gstatic.com/firebasejs/10.8.0/firebase-firestore.js";
import {
    getAuth,
    GoogleAuthProvider,
    signInWithPopup,
    signOut as firebaseSignOut,
    onAuthStateChanged
} from "https://www.gstatic.com/firebasejs/10.8.0/firebase-auth.js";
import {
    getStorage,
    ref,
    uploadBytes,
    getDownloadURL,
    deleteObject
} from "https://www.gstatic.com/firebasejs/10.8.0/firebase-storage.js";

// Firebase Configuration
const firebaseConfig = {
    apiKey: "AIzaSyDar5ZlJA9iEuGjAw-vT2U2Hlbl0NT4AAQ",
    authDomain: "storhaugenwebsite.firebaseapp.com",
    projectId: "storhaugenwebsite",
    storageBucket: "storhaugenwebsite.firebasestorage.app",
    messagingSenderId: "55384801378",
    appId: "1:55384801378:web:a85286c573dc614f46513b",
    measurementId: "G-ZSQ7TJKCN7"
};

// Initialize Firebase
const app = initializeApp(firebaseConfig);
const db = getFirestore(app);
const auth = getAuth(app);
const storage = getStorage(app);

// ============ AUTH FUNCTIONS ============

export async function loginWithGoogle() {
    const provider = new GoogleAuthProvider();
    try {
        const result = await signInWithPopup(auth, provider);
        return result.user.email;
    } catch (error) {
        console.error("Login Error:", error);
        return null;
    }
}

export async function getCurrentUserEmail() {
    return auth.currentUser?.email || null;
}

export async function signOut() {
    try {
        await firebaseSignOut(auth);
        return true;
    } catch (error) {
        console.error("Sign out error:", error);
        return false;
    }
}

// ============ FOOD ITEM FUNCTIONS ============

export async function addFoodItem(foodData) {
    try {
        const docRef = await addDoc(collection(db, "foods"), {
            name: foodData.name,
            description: foodData.description || "",
            imageUrls: foodData.imageUrls || [],
            ratings: foodData.ratings || { Markus: null, Siv: null, Elias: null },
            dateAdded: foodData.dateAdded || new Date().toISOString(),
            addedBy: foodData.addedBy || "",
            isArchived: false,
            archivedDate: null,
            archivedBy: null
        });
        return docRef.id;
    } catch (e) {
        console.error("Error adding document: ", e);
        throw e;
    }
}

export async function updateFoodItem(foodData) {
    try {
        const docRef = doc(db, "foods", foodData.id);
        await updateDoc(docRef, {
            name: foodData.name,
            description: foodData.description || "",
            imageUrls: foodData.imageUrls || [],
            ratings: foodData.ratings,
            dateAdded: foodData.dateAdded,
            addedBy: foodData.addedBy,
            isArchived: foodData.isArchived || false,
            archivedDate: foodData.archivedDate || null,
            archivedBy: foodData.archivedBy || null
        });
        return true;
    } catch (e) {
        console.error("Error updating document: ", e);
        throw e;
    }
}

export async function getFoodItems(includeArchived = false) {
    try {
        // SIMPLIFIED: Just get everything sorted by date.
        // We will filter 'isArchived' in C# or let the UI handle it.
        // This avoids the need for a composite index and shows old data 
        // that might be missing the 'isArchived' field.
        const q = query(
            collection(db, "foods"),
            orderBy("dateAdded", "desc")
        );

        const querySnapshot = await getDocs(q);
        let foods = [];
        querySnapshot.forEach((doc) => {
            const data = doc.data();

            // Only add to list if we want archived, OR if the item is NOT archived
            // We treat 'undefined' (missing field) as false (not archived)
            const isItemArchived = data.isArchived === true;

            if (includeArchived || !isItemArchived) {
                foods.push({
                    id: doc.id,
                    name: data.name,
                    description: data.description,
                    imageUrls: data.imageUrls || [],
                    ratings: data.ratings || { Markus: null, Siv: null, Elias: null },
                    dateAdded: data.dateAdded,
                    addedBy: data.addedBy,
                    isArchived: isItemArchived,
                    archivedDate: data.archivedDate,
                    archivedBy: data.archivedBy
                });
            }
        });
        return foods;
    } catch (e) {
        console.error("Error getting documents: ", e);
        return [];
    }
}

export async function getFoodItemById(id) {
    try {
        const docRef = doc(db, "foods", id);
        const docSnap = await getDoc(docRef);
        if (docSnap.exists()) {
            const data = docSnap.data();
            return {
                id: docSnap.id,
                name: data.name,
                description: data.description,
                imageUrls: data.imageUrls || [],
                ratings: data.ratings || { Markus: null, Siv: null, Elias: null },
                dateAdded: data.dateAdded,
                addedBy: data.addedBy,
                isArchived: data.isArchived || false,
                archivedDate: data.archivedDate,
                archivedBy: data.archivedBy
            };
        }
        return null;
    } catch (e) {
        console.error("Error getting document: ", e);
        return null;
    }
}

export async function archiveFoodItem(id, archivedBy, archivedDate) {
    try {
        const docRef = doc(db, "foods", id);
        await updateDoc(docRef, {
            isArchived: true,
            archivedBy: archivedBy,
            archivedDate: archivedDate
        });
        return true;
    } catch (e) {
        console.error("Error archiving document: ", e);
        throw e;
    }
}

export async function restoreFoodItem(id) {
    try {
        const docRef = doc(db, "foods", id);
        await updateDoc(docRef, {
            isArchived: false,
            archivedBy: null,
            archivedDate: null
        });
        return true;
    } catch (e) {
        console.error("Error restoring document: ", e);
        throw e;
    }
}

// ============ IMAGE FUNCTIONS ============

export async function uploadImage(base64Data, fileName) {
    try {
        // Convert base64 to blob
        const byteCharacters = atob(base64Data);
        const byteNumbers = new Array(byteCharacters.length);
        for (let i = 0; i < byteCharacters.length; i++) {
            byteNumbers[i] = byteCharacters.charCodeAt(i);
        }
        const byteArray = new Uint8Array(byteNumbers);
        const blob = new Blob([byteArray], { type: 'image/jpeg' });

        // Create unique filename
        const uniqueFileName = `food-images/${Date.now()}_${fileName}`;
        const storageRef = ref(storage, uniqueFileName);

        // Upload
        await uploadBytes(storageRef, blob);

        // Get download URL
        const downloadURL = await getDownloadURL(storageRef);
        return downloadURL;
    } catch (e) {
        console.error("Error uploading image: ", e);
        throw e;
    }
}

export async function deleteImage(imageUrl) {
    try {
        // Extract path from URL
        const storageRef = ref(storage, imageUrl);
        await deleteObject(storageRef);
        return true;
    } catch (e) {
        console.error("Error deleting image: ", e);
        // Don't throw - image might already be deleted
        return false;
    }
}