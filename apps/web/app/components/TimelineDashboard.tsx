'use client'

import { useEffect, useMemo, useState } from 'react'
import type { CSSProperties } from 'react'

type Activity = {
  id?: string
  title: string
  author: string
  image?: string
}

type Character = {
  id?: string
  name: string
  author: string
  image?: string
}

type Props = {
  apiBaseUrl?: string | null
  initialActivities: Activity[]
  initialActivityError: boolean
  initialRecentCharacters: Character[]
  initialRecentCharacterError: boolean
}

const MAX_CHARACTERS = 10
const isImageUrl = (value?: string) =>
  !!value && (/^https?:\/\//i.test(value) || value.startsWith('/'))

async function fetchList(url: string) {
  try {
    const response = await fetch(url)
    if (!response.ok) return null
    const json = await response.json()
    if (Array.isArray(json)) return json
    if (json && typeof json === 'object') {
      if (Array.isArray((json as { items?: unknown[] }).items)) return (json as { items: unknown[] }).items
      if ((json as { ok?: boolean; data?: unknown }).ok && (json as { data?: unknown }).data) {
        const data = (json as { data: unknown }).data
        if (Array.isArray(data)) return data
        if (Array.isArray((data as { items?: unknown[] }).items)) return (data as { items: unknown[] }).items
      }
    }
    return null
  } catch {
    return null
  }
}

function mapActivities(items: unknown[] | null): Activity[] | null {
  if (!items) return null
  return items.map((item, index) => {
    const data = item as Record<string, unknown>
    return {
      id: String(data.id ?? ''),
      title:
        String(
          data.title ??
            data.name ??
            data.summary ??
            data.label ??
            `Activity ${index + 1}`
        ),
      author: String(
        data.author ??
          data.creator_name ??
          data.user_name ??
          data.owner_name ??
          'Unknown'
      ),
      image: String(
        data.image ??
          data.image_url ??
          data.thumbnail_url ??
          data.cover_url ??
          data.media_url ??
          ''
      ),
    }
  })
}

function mapCharacters(items: unknown[] | null): Character[] | null {
  if (!items) return null
  return items.map((item, index) => {
    const data = item as Record<string, unknown>
    return {
      id: String(data.id ?? ''),
      name: String(
        data.name ??
          data.character_name ??
          data.title ??
          data.label ??
          `Character ${index + 1}`
      ),
      author: String(
        data.author ??
          data.owner_name ??
          data.user_name ??
          data.creator_name ??
          'Unknown'
      ),
      image: String(
        data.image ??
          data.image_url ??
          data.thumbnail_url ??
          data.avatar_url ??
          data.cover_url ??
          ''
      ),
    }
  })
}

export default function TimelineDashboard({
  apiBaseUrl,
  initialActivities,
  initialActivityError,
  initialRecentCharacters,
  initialRecentCharacterError,
}: Props) {
  const [activities, setActivities] = useState<Activity[]>(initialActivities)
  const [activityFetchFailed, setActivityFetchFailed] = useState(initialActivityError)
  const [activityFilter, setActivityFilter] = useState('all')
  const [hotCharacters, setHotCharacters] = useState<Character[]>([])
  const [mostViewedCharacters, setMostViewedCharacters] = useState<Character[]>([])
  const [recentCharacters, setRecentCharacters] = useState<Character[]>(initialRecentCharacters)
  const [randomCharacters, setRandomCharacters] = useState<Character[]>([])
  const [hotFetchFailed, setHotFetchFailed] = useState(false)
  const [mostViewedFetchFailed, setMostViewedFetchFailed] = useState(false)
  const [recentFetchFailed, setRecentFetchFailed] = useState(initialRecentCharacterError)
  const [randomFetchFailed, setRandomFetchFailed] = useState(false)

  const baseUrl = useMemo(() => apiBaseUrl?.replace(/\/$/, ''), [apiBaseUrl])

  useEffect(() => {
    if (!baseUrl) return
    let mounted = true
    const activityQuery =
      activityFilter === 'all'
        ? ''
        : `?media_type=${encodeURIComponent(activityFilter)}`
    Promise.all([
      fetchList(`${baseUrl}/activities${activityQuery}`),
      fetchList(`${baseUrl}/characters?sort=hot`),
      fetchList(`${baseUrl}/characters?sort=most_viewed`),
      fetchList(`${baseUrl}/characters?sort=recently_updated`),
      fetchList(`${baseUrl}/characters?sort=random`),
    ]).then(([activityItems, hotItems, mostViewedItems, recentItems, randomItems]) => {
      if (!mounted) return
      const mappedActivities = mapActivities(activityItems)
      const mappedHot = mapCharacters(hotItems)
      const mappedMostViewed = mapCharacters(mostViewedItems)
      const mappedRecent = mapCharacters(recentItems)
      const mappedRandom = mapCharacters(randomItems)
      if (mappedActivities) {
        setActivities(mappedActivities)
        setActivityFetchFailed(false)
      } else {
        setActivityFetchFailed(true)
      }
      if (mappedHot) {
        setHotCharacters(mappedHot)
        setHotFetchFailed(false)
      } else {
        setHotFetchFailed(true)
      }
      if (mappedMostViewed) {
        setMostViewedCharacters(mappedMostViewed)
        setMostViewedFetchFailed(false)
      } else {
        setMostViewedFetchFailed(true)
      }
      if (mappedRecent) {
        setRecentCharacters(mappedRecent)
        setRecentFetchFailed(false)
      } else {
        setRecentFetchFailed(true)
      }
      if (mappedRandom) {
        setRandomCharacters(mappedRandom)
        setRandomFetchFailed(false)
      } else {
        setRandomFetchFailed(true)
      }
    })

    return () => {
      mounted = false
    }
  }, [baseUrl, activityFilter])

  return (
    <div className="content">
      <section className="section wide">
        <div className="container">
          <div className="section-header">
            <div className="filters">
              {['all', 'illustration', 'writing', 'audio', '3d'].map((type) => (
                <button
                  className={`filter-chip ${activityFilter === type ? 'is-active' : ''}`}
                  key={type}
                  type="button"
                  onClick={() => setActivityFilter(type)}
                >
                  {type === 'all' ? 'All' : type[0].toUpperCase() + type.slice(1)}
                </button>
              ))}
              <a className="btn secondary" href="#">
                View all activities
              </a>
            </div>
          </div>
          <div className="scroll-wrap compact">
            <div className="scroll-row">
              {activityFetchFailed ? (
                <article className="card error-card">
                  <div>
                    <strong>Fetch failed</strong>
                    <span>Activities</span>
                  </div>
                </article>
              ) : activities.length === 0 ? (
                <article className="card empty-card">
                  <span className="empty-plus">+</span>
                </article>
              ) : (
                activities.map((activity, index) => (
                  <article
                    className="card animate-in delay-2"
                    key={activity.id ? `activity-${activity.id}` : `activity-${index}`}
                  >
                    <div className="card-media">
                      <div
                        className="media-frame"
                        style={{
                          '--media-image': isImageUrl(activity.image)
                            ? `url("${activity.image}")`
                            : 'none',
                        } as CSSProperties}
                      />
                      <div className="media-overlay">
                        <strong>{activity.title}</strong>
                        <span>by {activity.author}</span>
                      </div>
                    </div>
                  </article>
                ))
              )}
            </div>
          </div>
        </div>
      </section>

      <section className="section wide">
        <div className="container">
          <div className="section-header">
            <div>
              <h2>Characters</h2>
            </div>
            <div className="filters">
              <a className="btn secondary" href="#">
                View all characters
              </a>
            </div>
          </div>
          {[
            {
              label: 'Hot',
              items: hotCharacters,
              failed: hotFetchFailed,
              link: '#',
            },
            {
              label: 'Most Viewed',
              items: mostViewedCharacters,
              failed: mostViewedFetchFailed,
              link: '#',
            },
            {
              label: 'Recently Updated',
              items: recentCharacters,
              failed: recentFetchFailed,
              link: '#',
            },
            {
              label: 'Random',
              items: randomCharacters,
              failed: randomFetchFailed,
              link: '#',
            },
          ].map((group) => (
            <div className="character-row" key={group.label}>
              <div className="row-title">{group.label}</div>
              <div className="scroll-wrap compact">
                <div className="scroll-row">
                  {group.failed ? (
                    <article className="card error-card">
                      <div>
                        <strong>Fetch failed</strong>
                        <span>{group.label}</span>
                      </div>
                    </article>
                  ) : group.items.length === 0 ? (
                    <article className="card empty-card">
                      <span className="empty-plus">+</span>
                    </article>
                  ) : (
                    <>
                      {group.items.slice(0, MAX_CHARACTERS).map((character, index) => (
                        <article
                          className="card animate-in delay-2"
                          key={
                            character.id
                              ? `character-${group.label}-${character.id}`
                              : `character-${group.label}-${index}`
                          }
                        >
                          <div className="card-media">
                            <div
                              className="media-frame"
                              style={{
                                '--media-image': isImageUrl(character.image)
                                  ? `url("${character.image}")`
                                  : 'none',
                              } as CSSProperties}
                            />
                            <div className="media-overlay">
                              <strong>{character.name}</strong>
                              <span>by {character.author}</span>
                            </div>
                          </div>
                        </article>
                      ))}
                      <article
                        className="card more-card"
                        key={`character-${group.label}-more`}
                      >
                        <a className="more-link" href={group.link}>
                          <span>View all</span>
                          <strong>+</strong>
                        </a>
                      </article>
                    </>
                  )}
                </div>
              </div>
            </div>
          ))}
        </div>
      </section>

      <footer className="footer">
        <div className="container footer-inner">
          <div>
            <strong>Ori-Chara Chronicle</strong>
            <p>Context-first archive for collaborative character worlds.</p>
          </div>
          <div className="footer-links">
            <span>Docs</span>
            <span>Status</span>
            <span>Contact</span>
          </div>
        </div>
      </footer>
    </div>
  )
}
