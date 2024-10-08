import json
import time
from collections import OrderedDict
from datetime import datetime, timezone
from getpass import getpass
from pathlib import Path
from typing import List
import sys

import dateutil.parser
from selenium import webdriver
from selenium.common.exceptions import NoSuchElementException
from selenium.webdriver import ActionChains
from selenium.webdriver.common.by import By
from selenium.webdriver.remote.webelement import WebElement

### MAKE CHANGES HERE!!!
delete_artists = [sys.argv[1]]
delete_songs = []
### END CHANGES

if "y" == sys.argv[2].lower():
    trial_run = True
else:
    trial_run = False

use_hours = True
middle_hour = int(sys.argv[3])
start_hour = middle_hour - 100
stop_hour = middle_hour + 100
use_year = False

username = sys.argv[4]
password = sys.argv[5]

login_page = "https://www.last.fm/login"
user_library_page = f"https://www.last.fm/user/{username}/library"
page_num = 0
page_url = f"{user_library_page}?page={page_num}"

deleted_dict = OrderedDict()
deleted_dict["deleted"] = []

save_file = None
if trial_run:
    save_file = Path(f"output/output_{datetime.now()} (trial).json".replace(":", "꞉"))
else:
    save_file = Path(f"output/output_{datetime.now()}.json".replace(":", "꞉"))

if not Path("output").exists():
    Path("output").mkdir(parents = True)

save_file.touch(exist_ok = True)


def save(deleted_dict):
    save_file.write_text(json.dumps(deleted_dict, ensure_ascii = False, indent = 4), encoding = 'utf8')


def should_delete(track_name, artist_name, parsed_timestamp):
    delete = False

    # Delete if track matches artist/track name list
    if artist_name in delete_artists:
        delete = True
    if track_name in delete_songs:
        delete = True

    if delete:
        # Don't delete if track is outside of specified time
        if use_hours:
            if start_hour > stop_hour:
                if datetime.timestamp(parsed_timestamp) >= start_hour or datetime.timestamp(parsed_timestamp) <= stop_hour:
                    delete = True
                else:
                    delete = False
            elif stop_hour > start_hour:
                if start_hour <= datetime.timestamp(parsed_timestamp) <= stop_hour:
                    delete = True
                else:
                    delete = False
            else:
                if datetime.timestamp(parsed_timestamp) == start_hour:
                    delete = True
                else:
                    delete = False

    return delete

# Check if there are scrobbles to delete on the page
def to_delete_exists(driver):
    sections: List[WebElement] = driver.find_elements(by = By.CSS_SELECTOR, value = "section.tracklist-section")
    section: WebElement
    for section in sections:
        table: WebElement = section.find_element(by = By.TAG_NAME, value = "table")
        table_body: WebElement = table.find_element(by = By.TAG_NAME, value = "tbody")

        row_num = 0
        for row in table_body.find_elements(by = By.TAG_NAME, value = "tr"):
            row_num += 1
            track_name = row.find_element(by = By.CLASS_NAME, value = "chartlist-name").find_element(by = By.TAG_NAME, value = "a").text
            artist_name = row.find_element(by = By.CLASS_NAME, value = "chartlist-artist").find_element(by = By.TAG_NAME, value = "a").text
            timestamp = row.find_element(by = By.CLASS_NAME, value = "chartlist-timestamp").find_element(by = By.TAG_NAME, value = "span")
            timestamp_string = timestamp.get_attribute("title")
            parsed_timestamp = dateutil.parser.parse(timestamp_string)

            delete = should_delete(track_name = track_name, artist_name = artist_name, parsed_timestamp = parsed_timestamp)
            if delete:
                return True

    return False



print()
print("Launching Firefox")
with webdriver.Firefox() as driver:
    driver.get(login_page)
    driver.find_element(by = By.ID, value = "id_username_or_email").send_keys(username)
    driver.find_element(by = By.ID, value = "id_password").send_keys(password)

    print("Accept the cookies popup! You have 10 seconds to do so.")
    time.sleep(10)

    from selenium.webdriver.support import expected_conditions
    from selenium.webdriver.support.wait import WebDriverWait

    WebDriverWait(driver, 10).until(expected_conditions.visibility_of_element_located((By.CSS_SELECTOR, "button[name='submit']")))
    while True:
        try:
            driver.find_element(by = By.CSS_SELECTOR, value = "button[name='submit']").click()
            break
        except:
            print("\nERROR! Can't access the webpage. You need to accept the cookies popup!")
            print("(Ctrl+C to stop the program and exit this retry loop)")
            for count in range(10):
                print("Will try again in: " + str(10 - count))
                time.sleep(1)

    driver.get(page_url)

    while True:
        print(f"On page #{page_num}")
        attempt = 0
        while to_delete_exists(driver):
            attempt += 1
            print(f"Attempt #{attempt}")
            sections: List[WebElement] = driver.find_elements(by = By.CSS_SELECTOR, value = "section.tracklist-section")
            section: WebElement
            for section in sections:
                table: WebElement = section.find_element(by = By.TAG_NAME, value = "table")
                table_body: WebElement = table.find_element(by = By.TAG_NAME, value = "tbody")

                row_num = 0
                for row in table_body.find_elements(by = By.TAG_NAME, value = "tr"):
                    row_num += 1
                    track_name = row.find_element(by = By.CLASS_NAME, value = "chartlist-name").find_element(by = By.TAG_NAME, value = "a").text
                    artist_name = row.find_element(by = By.CLASS_NAME, value = "chartlist-artist").find_element(by = By.TAG_NAME, value = "a").text
                    timestamp = row.find_element(by = By.CLASS_NAME, value = "chartlist-timestamp").find_element(by = By.TAG_NAME, value = "span")
                    timestamp_string = timestamp.get_attribute("title")
                    parsed_timestamp = dateutil.parser.parse(timestamp_string)

                    if should_delete(track_name = track_name, artist_name = artist_name, parsed_timestamp = parsed_timestamp):
                        chartlist_more = row.find_element(by = By.CLASS_NAME, value = "chartlist-more")
                        driver.execute_script(f"window.scrollTo(0, {timestamp.location['y'] - 100})")
                        ActionChains(driver).move_to_element_with_offset(timestamp, 0, 100).perform()
                        timestamp.click()
                        while True:
                            try:
                                if not trial_run:
                                    chartlist_more.find_element(by = By.CLASS_NAME, value = "chartlist-more-button").click()
                                    time.sleep(0.05)
                                    # chartlist_more.find_element(by = By.CLASS_NAME, value = "chartlist-more-button").click()
                                    chartlist_more.find_element(by = By.CLASS_NAME, value = "more-item--delete").click()
                                    time.sleep(0.05)
                                break
                            except Exception as e:
                                print(f"Problem deleting {track_name} ({artist_name}) ({timestamp_string}). This should be fixed automatically.")
                                save(deleted_dict)

                        deleted_object_dict = OrderedDict()
                        deleted_object_dict["track_name"] = track_name
                        deleted_object_dict["artist_name"] = artist_name
                        deleted_object_dict["timestamp"] = timestamp_string
                        deleted_object_dict["readable_timestamp"] = str(parsed_timestamp)

                        deleted_dict["deleted"].append(deleted_object_dict)
                        sys.exit()

            if trial_run:
                break
            driver.get(f"https://www.last.fm/user/{username}/library?page={page_num}")

        try:
            next_button = driver.find_element(by = By.CSS_SELECTOR, value = ".pagination-next > a:nth-child(1)")
        except NoSuchElementException:
            print("Finished last page! Exiting.")
            break

        save(deleted_dict)

        driver.get(next_button.get_attribute("href"))
        page_num += 1

save(deleted_dict)
