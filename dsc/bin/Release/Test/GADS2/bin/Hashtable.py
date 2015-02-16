from ArrayList import *
from ITable import *

class Hashtable(ITable):
    """ Represents a hash table that uses separate chaining. """

    def __init__(self, KeyMap, BucketFactory):
        """ Creates a new hash table, from the provided key map and the bucket table factory. """
        # Remarks:
        # 'BucketFactory' of type 'IFactory<ITable<TKey, TValue>, IMap<TValue, TKey>>' is a factory that creates instances of 'ITable<TKey, TValue>' when provided an argument of type 'IMap<TValue, TKey>'.
        # Essentially, it creates new buckets, which are themselves tables, from a key-value map.
        self.bucket_count = 0
        self.prime_list = [31, 97, 389, 1543, 6151, 24593, 98317, 393241, 1572869, 6291469, 25165843, 100663319, 402653189, 1610612741]
        self.buckets = None
        self.key_map = KeyMap
        self.bucket_factory = BucketFactory
        self.buckets = [None] * self.prime_list[0]

    def get_next_prime(self):
        """ Gets the next prime in the prime list.
            If this prime is not available, -1 is returned.
            This method is private. """
        i = 0
        while i < len(self.prime_list) - 1:
            if self.prime_list[i] > self.bucket_capacity:
                return self.prime_list[i]
            i += 1
        return -1

    def resize_table(self):
        """ Tries to resize the table to the next prime and re-hashes every element.
            This method is private. """
        nextPrime = self.get_next_prime()
        if nextPrime > -1:
            oldBuckets = self.buckets
            self.buckets = [None] * nextPrime
            self.bucket_count = 0
            for i in range(len(oldBuckets)):
                if oldBuckets[i] is not None:
                    for item in oldBuckets[i]:
                        self.insert(item)

    def insert(self, Item):
        """ Inserts an item in the hash table. """
        # Post:
        # Returns true if item is successfully inserted, false if the table already contains an item with the same search key.
        key = self.key_map.map(Item)
        hashCode = hash(key)
        bucket = self.get_new_bucket(hashCode)
        if self.bucket_contains_key(bucket, key):
            return False
        bucket.insert(Item)
        if self.bucket_load_factor > 0.75:
            self.resize_table()
        return True

    def get_new_bucket(self, HashCode):
        """ Gets the bucket for items with the given hash code or creates a new one, if necessary.
            This method is private. """
        index = HashCode % self.bucket_capacity
        bucket = self.buckets[index]
        if bucket is None:
            self.bucket_count += 1
            bucket = self.bucket_factory.create(self.key_map)
            self.buckets[index] = bucket
        return bucket

    def bucket_contains_key(self, Bucket, Key):
        """ Finds out if a bucket contains the given key.
            This method is private. """
        return self.find_in_bucket(Bucket, Key) is not None

    def find_in_bucket(self, Bucket, Key):
        """ Finds an item in the given bucket with the given key.
            This method is private. """
        if Bucket is None:
            return None
        return Bucket[Key]

    def get_bucket(self, HashCode):
        """ Gets the bucket for items with the given hash code.
            This method is private. """
        index = HashCode % self.bucket_capacity
        bucket = self.buckets[index]
        return bucket

    def delete_bucket(self, HashCode):
        """ Deletes the bucket with the provided hash code.
            This method is private. """
        index = HashCode % self.bucket_capacity
        self.buckets[index] = None
        self.bucket_count -= 1

    def contains_key(self, Key):
        """ Gets a boolean value that indicates if the hash table contains the given key. """
        return self.bucket_contains_key(self.get_bucket(hash(Key)), Key)

    def remove(self, Key):
        """ Removes a key from the table. """
        # Post:
        # This method returns true if the key is in the table, false if not.
        hashCode = hash(Key)
        bucket = self.get_bucket(hashCode)
        if bucket is None:
            return False
        result = bucket.remove(Key)
        if bucket.count == 0:
            self.delete_bucket(hashCode)
        return result

    def __iter__(self):
        """ Creates an iterator that iterates over every element in the collection. """
        for i in range(len(self.buckets)):
            if self.buckets[i] is not None:
                for item in self.buckets[i]:
                    yield item

    def to_list(self):
        """ Gets the table's items as a read-only list.
            The elements in this list are in the same order as those in the table's iterator, obtained through '__iter__' (the get iterator method).
            Any statement that applies to this method therefore also applies to the '__iter__' (get iterator) method, and vice-versa. """
        # Post:
        # This method returns a read-only list that describes the items in this table.
        # Modifications to this list are not allowed - it is read-only.
        # Furthermore, this list may be an alias to an internal list containing the table's items, or a copy.
        # This list need not be sorted, but must contain every item in the table.
        results = ArrayList()
        for i in range(len(self.buckets)):
            if self.buckets[i] is not None:
                for item in self.buckets[i]:
                    results.add(item)
        return results

    @property
    def bucket_capacity(self):
        """ Gets the number of buckets in the table.
            This accessor is private. """
        return len(self.buckets)

    @property
    def key_map(self):
        """ Gets the record-to-key mapping function used by this hash table. """
        return self.key_map_value

    @key_map.setter
    def key_map(self, value):
        """ Sets the record-to-key mapping function used by this hash table.
            This accessor is private. """
        self.key_map_value = value

    @property
    def bucket_factory(self):
        """ Gets the factory that is used to create new buckets for this hash table. """
        return self.bucket_factory_value

    @bucket_factory.setter
    def bucket_factory(self, value):
        """ Sets the factory that is used to create new buckets for this hash table.
            This accessor is private. """
        self.bucket_factory_value = value

    @property
    def bucket_load_factor(self):
        """ Gets the bucket load factor.
            This accessor is private. """
        return self.bucket_count / self.bucket_capacity

    @property
    def count(self):
        """ Gets the number of elements in the collection. """
        result = 0
        for i in range(len(self.buckets)):
            if self.buckets[i] is not None:
                result += self.buckets[i].count
        return result

    @property
    def is_empty(self):
        for i in range(len(self.buckets)):
            if self.buckets[i] is not None and self.buckets[i].count > 0:
                return False
        return True

    def __getitem__(self, Key):
        """ Retrieves the item in the table with the specified key. """
        # Pre:
        # For this method to return an item in the table, rather than null, the key must be in the table, i.e.
        # ContainsKey(Key) must return true.
        # Post:
        # The return value of this method will be the item that corresponds with the key, or None, if it is not found.
        # It is recommended to check if the table contains the key by using ContainsKey.
        bucket = self.get_bucket(hash(Key))
        return self.find_in_bucket(bucket, Key)